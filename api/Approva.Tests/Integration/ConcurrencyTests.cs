using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace Approva.Tests.Integration;

/// <summary>Proves the optimistic-concurrency story from the plan: two approvers
/// deciding the same task at the same time — one wins with 200, the other loses with
/// 409 Conflict instead of silently double-processing the request.</summary>
public class ConcurrencyTests : IClassFixture<ApprovaWebApplicationFactory>
{
    private readonly ApprovaWebApplicationFactory _factory;

    public ConcurrencyTests(ApprovaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private record AuthResult(string Token, Guid UserId, Guid TenantId, string Email, string Name, string Role);

    private HttpClient AuthedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task TwoSimultaneousApprovals_OneSucceedsOneConflicts()
    {
        var slug = $"conc-{Guid.NewGuid():N}"[..12];
        var adminClient = _factory.CreateClient();

        var registerResponse = await adminClient.PostAsJsonAsync("/auth/register-tenant", new
        {
            tenantName = "Concurrency Co",
            tenantSlug = slug,
            adminName = "Admin",
            adminEmail = $"admin@{slug}.test",
            adminPassword = "TestPass123!"
        });
        registerResponse.EnsureSuccessStatusCode();
        var admin = await registerResponse.Content.ReadFromJsonAsync<AuthResult>();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin!.Token);

        var managerResponse = await adminClient.PostAsJsonAsync("/users", new
        {
            email = $"manager@{slug}.test",
            name = "Manager",
            password = "TestPass123!",
            role = "Approver",
            approverRole = (string?)null,
            managerId = (Guid?)null
        });
        managerResponse.EnsureSuccessStatusCode();
        var managerId = (await managerResponse.Content.ReadFromJsonAsync<Dictionary<string, Guid>>())!["id"];

        var requesterResponse = await adminClient.PostAsJsonAsync("/users", new
        {
            email = $"requester@{slug}.test",
            name = "Requester",
            password = "TestPass123!",
            role = "Requester",
            approverRole = (string?)null,
            managerId
        });
        requesterResponse.EnsureSuccessStatusCode();

        var workflowResponse = await adminClient.PostAsJsonAsync("/workflow-definitions", new
        {
            name = "Compras",
            entityType = "PurchaseRequest",
            steps = new[]
            {
                new
                {
                    name = "Manager",
                    approverType = "Manager",
                    approverRef = (string?)null,
                    slaHours = 24,
                    escalationPolicy = "None",
                    conditions = Array.Empty<object>()
                }
            }
        });
        workflowResponse.EnsureSuccessStatusCode();
        var workflowId = (await workflowResponse.Content.ReadFromJsonAsync<Dictionary<string, Guid>>())!["id"];

        var requesterLogin = await adminClient.PostAsJsonAsync("/auth/login",
            new { email = $"requester@{slug}.test", password = "TestPass123!" });
        requesterLogin.EnsureSuccessStatusCode();
        var requesterAuth = await requesterLogin.Content.ReadFromJsonAsync<AuthResult>();
        using var requesterClient = AuthedClient(requesterAuth!.Token);

        var createResponse = await requesterClient.PostAsJsonAsync("/requests", new
        {
            workflowDefinitionId = workflowId,
            title = "Compra concurrente",
            amount = 500,
            currency = "USD",
            payloadJson = "{}"
        });
        createResponse.EnsureSuccessStatusCode();
        var requestId = (await createResponse.Content.ReadFromJsonAsync<Dictionary<string, Guid>>())!["id"];

        var managerLogin = await adminClient.PostAsJsonAsync("/auth/login",
            new { email = $"manager@{slug}.test", password = "TestPass123!" });
        managerLogin.EnsureSuccessStatusCode();
        var managerAuth = await managerLogin.Content.ReadFromJsonAsync<AuthResult>();

        using var managerClientA = AuthedClient(managerAuth!.Token);
        using var managerClientB = AuthedClient(managerAuth.Token);

        var decisionBody = new { decision = "Approve", comment = "concurrent" };

        var taskA = managerClientA.PostAsJsonAsync($"/requests/{requestId}/decisions", decisionBody);
        var taskB = managerClientB.PostAsJsonAsync($"/requests/{requestId}/decisions", decisionBody);
        var results = await Task.WhenAll(taskA, taskB);

        var statusCodes = results.Select(r => r.StatusCode).OrderBy(c => c).ToList();

        Assert.Contains(HttpStatusCode.OK, statusCodes);
        Assert.Contains(HttpStatusCode.Conflict, statusCodes);
    }
}
