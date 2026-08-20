using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace Approva.Tests.Integration;

/// <summary>The test the plan calls out by name: create two tenants, have one try to
/// read the other's data, and prove it fails. Runs against a real Postgres container
/// through the real HTTP pipeline (auth, EF Core global query filters, everything) —
/// not a mock.</summary>
public class TenantIsolationTests : IClassFixture<ApprovaWebApplicationFactory>
{
    private readonly ApprovaWebApplicationFactory _factory;

    public TenantIsolationTests(ApprovaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private record AuthResult(string Token, Guid UserId, Guid TenantId, string Email, string Name, string Role);

    private async Task<(HttpClient Client, AuthResult Auth)> RegisterTenantAsync(string slug)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/register-tenant", new
        {
            tenantName = $"Tenant {slug}",
            tenantSlug = slug,
            adminName = $"Admin {slug}",
            adminEmail = $"admin@{slug}.test",
            adminPassword = "TestPass123!"
        });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return (client, auth);
    }

    // Uses SpecificUser (approverAndRequesterId) rather than Manager, so the same admin
    // account created by RegisterTenant — which has no ManagerId — can both submit and
    // approve the request without tripping the domain's "no manager assigned" guard.
    // This test only cares about tenant isolation, not routing rules.
    private static async Task<Guid> CreateWorkflowAsync(HttpClient adminClient, Guid approverAndRequesterId)
    {
        var response = await adminClient.PostAsJsonAsync("/workflow-definitions", new
        {
            name = "Compras",
            entityType = "PurchaseRequest",
            steps = new[]
            {
                new
                {
                    name = "Aprobación",
                    approverType = "SpecificUser",
                    approverRef = approverAndRequesterId.ToString(),
                    slaHours = 24,
                    escalationPolicy = "None",
                    conditions = Array.Empty<object>()
                }
            }
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        return body!["id"];
    }

    private static async Task<Guid> CreateRequestAsync(HttpClient client, Guid workflowId, string title)
    {
        var response = await client.PostAsJsonAsync("/requests", new
        {
            workflowDefinitionId = workflowId,
            title,
            amount = 100,
            currency = "USD",
            payloadJson = "{}"
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        return body!["id"];
    }

    [Fact]
    public async Task TenantB_CannotListTenantAsRequests()
    {
        var (clientA, authA) = await RegisterTenantAsync($"acme-{Guid.NewGuid():N}"[..12]);
        var workflowA = await CreateWorkflowAsync(clientA, authA.UserId);
        await CreateRequestAsync(clientA, workflowA, "Secreto de Acme");

        var (clientB, _) = await RegisterTenantAsync($"beta-{Guid.NewGuid():N}"[..12]);

        var response = await clientB.GetAsync("/requests");
        response.EnsureSuccessStatusCode();
        var requests = await response.Content.ReadFromJsonAsync<List<object>>();

        Assert.Empty(requests!);
    }

    [Fact]
    public async Task TenantB_CannotReadTenantAsRequestById()
    {
        var (clientA, authA) = await RegisterTenantAsync($"acme-{Guid.NewGuid():N}"[..12]);
        var workflowA = await CreateWorkflowAsync(clientA, authA.UserId);
        var requestId = await CreateRequestAsync(clientA, workflowA, "Secreto de Acme");

        var (clientB, _) = await RegisterTenantAsync($"beta-{Guid.NewGuid():N}"[..12]);

        var response = await clientB.GetAsync($"/requests/{requestId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TenantB_CannotSeeTenantAsWorkflowDefinitions()
    {
        var (clientA, authA) = await RegisterTenantAsync($"acme-{Guid.NewGuid():N}"[..12]);
        await CreateWorkflowAsync(clientA, authA.UserId);

        var (clientB, _) = await RegisterTenantAsync($"beta-{Guid.NewGuid():N}"[..12]);

        var response = await clientB.GetAsync("/workflow-definitions");
        response.EnsureSuccessStatusCode();
        var workflows = await response.Content.ReadFromJsonAsync<List<object>>();

        Assert.Empty(workflows!);
    }

    [Fact]
    public async Task TenantB_CannotSeeTenantAsUsers()
    {
        var (clientA, authA) = await RegisterTenantAsync($"acme-{Guid.NewGuid():N}"[..12]);
        var (clientB, _) = await RegisterTenantAsync($"beta-{Guid.NewGuid():N}"[..12]);

        var response = await clientB.GetAsync("/users");
        response.EnsureSuccessStatusCode();
        var users = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

        Assert.DoesNotContain(users!, u => u["email"].ToString() == authA.Email);
    }
}
