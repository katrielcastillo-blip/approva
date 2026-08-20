using System.Net.Http.Headers;
using System.Net.Http.Json;
using Approva.Domain.Enums;
using Approva.Infrastructure.BackgroundJobs;
using Approva.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Approva.Tests.Integration;

/// <summary>Proves the Fase 4 promise end to end: a task whose SLA (DueAt) has passed
/// gets escalated to the assignee's manager by SlaEscalationJob, with its own audited
/// task row — not just that the code compiles, but that running it actually reassigns
/// work. Manipulates DueAt directly via EF Core (the domain has no public "backdate SLA"
/// API — that would only exist to serve this test) then invokes the same job Hangfire
/// runs on its 15-minute schedule.</summary>
public class SlaEscalationTests : IClassFixture<ApprovaWebApplicationFactory>
{
    private readonly ApprovaWebApplicationFactory _factory;

    public SlaEscalationTests(ApprovaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private record AuthResult(string Token, Guid UserId, Guid TenantId, string Email, string Name, string Role);

    [Fact]
    public async Task OverdueTask_GetsEscalatedToAssigneesManager()
    {
        var slug = $"sla-{Guid.NewGuid():N}"[..12];
        var adminClient = _factory.CreateClient();

        var registerResponse = await adminClient.PostAsJsonAsync("/auth/register-tenant", new
        {
            tenantName = "Sla Co",
            tenantSlug = slug,
            adminName = "Admin",
            adminEmail = $"admin@{slug}.test",
            adminPassword = "TestPass123!"
        });
        registerResponse.EnsureSuccessStatusCode();
        var admin = await registerResponse.Content.ReadFromJsonAsync<AuthResult>();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin!.Token);

        var vpResponse = await adminClient.PostAsJsonAsync("/users", new
        {
            email = $"vp@{slug}.test",
            name = "VP",
            password = "TestPass123!",
            role = "Approver",
            approverRole = (string?)null,
            managerId = (Guid?)null
        });
        vpResponse.EnsureSuccessStatusCode();
        var vpId = (await vpResponse.Content.ReadFromJsonAsync<Dictionary<string, Guid>>())!["id"];

        var managerResponse = await adminClient.PostAsJsonAsync("/users", new
        {
            email = $"manager@{slug}.test",
            name = "Manager",
            password = "TestPass123!",
            role = "Approver",
            approverRole = (string?)null,
            managerId = vpId
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
                    escalationPolicy = "EscalateToManager",
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

        using var requesterClient = _factory.CreateClient();
        requesterClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", requesterAuth!.Token);

        var createResponse = await requesterClient.PostAsJsonAsync("/requests", new
        {
            workflowDefinitionId = workflowId,
            title = "Compra con SLA vencido",
            amount = 500,
            currency = "USD",
            payloadJson = "{}"
        });
        createResponse.EnsureSuccessStatusCode();
        var requestId = (await createResponse.Content.ReadFromJsonAsync<Dictionary<string, Guid>>())!["id"];

        // Push the task's DueAt into the past so the job treats it as overdue — the one
        // piece of test-only manipulation, done at the EF Core level since neither the
        // API nor the domain expose a way to backdate an SLA (rightly so).
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApprovaDbContext>();
            var task = await db.ApprovalTasks.IgnoreQueryFilters()
                .SingleAsync(t => t.RequestId == requestId && t.Status == ApprovalTaskStatus.Pending);
            db.Database.ExecuteSqlInterpolated(
                $"UPDATE approval_tasks SET \"DueAt\" = {DateTimeOffset.UtcNow.AddHours(-1)} WHERE \"Id\" = {task.Id}");
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var job = scope.ServiceProvider.GetRequiredService<SlaEscalationJob>();
            await job.RunAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApprovaDbContext>();
            var tasks = await db.ApprovalTasks.IgnoreQueryFilters()
                .Where(t => t.RequestId == requestId)
                .OrderBy(t => t.AssignedAt)
                .ToListAsync();

            Assert.Equal(2, tasks.Count);
            Assert.Equal(ApprovalTaskStatus.Escalated, tasks[0].Status);
            Assert.Equal(managerId, tasks[0].AssignedToUserId);
            Assert.Equal(ApprovalTaskStatus.Pending, tasks[1].Status);
            Assert.Equal(vpId, tasks[1].AssignedToUserId);
        }
    }
}
