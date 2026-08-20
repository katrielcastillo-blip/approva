using Approva.Application.Common.Interfaces;
using Approva.Domain.Entities;
using Approva.Domain.Enums;
using Approva.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Approva.Infrastructure.Persistence;

/// <summary>Seeds a fictitious demo tenant ("Acme Corp") with users, a real 3-step
/// purchase-approval workflow, and requests spread across every status, so the app is
/// demoable immediately without manual setup. Runs once — no-ops if any tenant exists.</summary>
public static class DbSeeder
{
    public const string DemoPassword = "Demo1234!";

    public static async Task SeedAsync(ApprovaDbContext db, IPasswordHasher hasher, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            logger.LogInformation("Seed omitido: ya existen tenants.");
            return;
        }

        var tenant = Tenant.Create("Acme Corp", "acme");
        db.Tenants.Add(tenant);

        var passwordHash = hasher.Hash(DemoPassword);

        var admin = User.Create(tenant.Id, "admin@acme.test", "Admin Acme", UserRole.Admin, passwordHash);
        var manager = User.Create(tenant.Id, "ana.gomez@acme.test", "Ana Gómez", UserRole.Approver, passwordHash, "Manager");
        var cfo = User.Create(tenant.Id, "carlos.pena@acme.test", "Carlos Peña", UserRole.Approver, passwordHash, "CFO");
        var ceo = User.Create(tenant.Id, "elena.ruiz@acme.test", "Elena Ruiz", UserRole.Approver, passwordHash, "CEO");
        var luis = User.Create(tenant.Id, "luis.fernandez@acme.test", "Luis Fernández", UserRole.Requester, passwordHash, managerId: manager.Id);
        var maria = User.Create(tenant.Id, "maria.torres@acme.test", "María Torres", UserRole.Requester, passwordHash, managerId: manager.Id);

        db.Users.AddRange(admin, manager, cfo, ceo, luis, maria);

        var workflow = WorkflowDefinition.Create(tenant.Id, "Compras y Gastos", "PurchaseRequest");
        workflow.AddStep("Aprobación Manager", ApproverType.Manager, null, slaHours: 24);
        var cfoStep = workflow.AddStep("Aprobación CFO", ApproverType.Role, "CFO", slaHours: 48);
        cfoStep.AddCondition("Amount", ConditionOperator.GreaterThan, "5000");
        var ceoStep = workflow.AddStep("Aprobación CEO", ApproverType.Role, "CEO", slaHours: 72);
        ceoStep.AddCondition("Amount", ConditionOperator.GreaterThan, "50000");
        db.WorkflowDefinitions.Add(workflow);

        var allUsers = new List<User> { admin, manager, cfo, ceo, luis, maria };

        // A spread of requests across every status and every step, so the inbox,
        // request-detail audit trail, and bottleneck analytics all have real data.
        SeedApprovedRequest(db, tenant.Id, workflow, luis, allUsers, "Laptops para el equipo de diseño", 3200, "Marketing");
        SeedApprovedRequest(db, tenant.Id, workflow, maria, allUsers, "Licencias de software anual", 8500, "IT");
        SeedApprovedRequest(db, tenant.Id, workflow, luis, allUsers, "Renovación de flota de vehículos", 75000, "Operaciones");

        SeedPendingAtStep(db, tenant.Id, workflow, maria, allUsers, "Mobiliario de oficina", 1800, "Admin", stepIndex: 0);
        SeedPendingAtStep(db, tenant.Id, workflow, luis, allUsers, "Consultoría de marketing digital", 12000, "Marketing", stepIndex: 1);
        SeedPendingAtStep(db, tenant.Id, workflow, maria, allUsers, "Adquisición de startup local", 120000, "Estrategia", stepIndex: 2);

        SeedRejectedRequest(db, tenant.Id, workflow, luis, allUsers, "Viaje de lujo a conferencia", 15000, "Ventas");

        SeedCancelledRequest(db, tenant.Id, workflow, maria, allUsers, "Merchandising para evento cancelado", 2200, "Marketing");

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seed completado: tenant 'acme' con {UserCount} usuarios y 8 solicitudes de ejemplo.", allUsers.Count);
    }

    private static Request NewRequest(Guid tenantId, WorkflowDefinition workflow, User requester, string title, decimal amount, string department) =>
        Request.Create(tenantId, workflow.Id, requester.Id, title, amount, "USD", $$"""{"Department":"{{department}}"}""");

    private static void SeedApprovedRequest(ApprovaDbContext db, Guid tenantId, WorkflowDefinition workflow, User requester,
        List<User> allUsers, string title, decimal amount, string department)
    {
        var request = NewRequest(tenantId, workflow, requester, title, amount, department);
        db.Requests.Add(request);
        db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, requester.Id, AuditEventType.RequestCreated));

        var step = WorkflowEngine.DetermineNextStep(workflow, request, null);
        request.Submit(step?.Id);

        while (step is not null)
        {
            var assigneeId = WorkflowEngine.ResolveApprover(step, requester, allUsers);
            var task = ApprovalTask.Create(request.Id, step.Id, assigneeId, step.SlaHours);
            db.ApprovalTasks.Add(task);
            task.Approve(assigneeId, "Aprobado.");
            db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, assigneeId, AuditEventType.TaskApproved));

            step = WorkflowEngine.DetermineNextStep(workflow, request, step.Id);
            request.AdvanceTo(step?.Id);
        }

        db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, requester.Id, AuditEventType.RequestApproved));
    }

    private static void SeedPendingAtStep(ApprovaDbContext db, Guid tenantId, WorkflowDefinition workflow, User requester,
        List<User> allUsers, string title, decimal amount, string department, int stepIndex)
    {
        var request = NewRequest(tenantId, workflow, requester, title, amount, department);
        db.Requests.Add(request);
        db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, requester.Id, AuditEventType.RequestCreated));

        var step = WorkflowEngine.DetermineNextStep(workflow, request, null);
        request.Submit(step?.Id);

        for (var i = 0; i < stepIndex && step is not null; i++)
        {
            var assigneeId = WorkflowEngine.ResolveApprover(step, requester, allUsers);
            var task = ApprovalTask.Create(request.Id, step.Id, assigneeId, step.SlaHours);
            db.ApprovalTasks.Add(task);
            task.Approve(assigneeId, "Aprobado.");
            db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, assigneeId, AuditEventType.TaskApproved));

            step = WorkflowEngine.DetermineNextStep(workflow, request, step.Id);
            request.AdvanceTo(step?.Id);
        }

        if (step is not null)
        {
            var assigneeId = WorkflowEngine.ResolveApprover(step, requester, allUsers);
            db.ApprovalTasks.Add(ApprovalTask.Create(request.Id, step.Id, assigneeId, step.SlaHours));
            db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, requester.Id, AuditEventType.TaskAssigned));
        }
    }

    private static void SeedRejectedRequest(ApprovaDbContext db, Guid tenantId, WorkflowDefinition workflow, User requester,
        List<User> allUsers, string title, decimal amount, string department)
    {
        var request = NewRequest(tenantId, workflow, requester, title, amount, department);
        db.Requests.Add(request);
        db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, requester.Id, AuditEventType.RequestCreated));

        var step = WorkflowEngine.DetermineNextStep(workflow, request, null)!;
        request.Submit(step.Id);

        var assigneeId = WorkflowEngine.ResolveApprover(step, requester, allUsers);
        var task = ApprovalTask.Create(request.Id, step.Id, assigneeId, step.SlaHours);
        db.ApprovalTasks.Add(task);
        task.Reject(assigneeId, "Fuera de política de gastos.");
        db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, assigneeId, AuditEventType.TaskRejected));

        request.Reject();
        db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, assigneeId, AuditEventType.RequestRejected));
    }

    private static void SeedCancelledRequest(ApprovaDbContext db, Guid tenantId, WorkflowDefinition workflow, User requester,
        List<User> allUsers, string title, decimal amount, string department)
    {
        var request = NewRequest(tenantId, workflow, requester, title, amount, department);
        db.Requests.Add(request);
        db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, requester.Id, AuditEventType.RequestCreated));

        var step = WorkflowEngine.DetermineNextStep(workflow, request, null);
        request.Submit(step?.Id);

        if (step is not null)
        {
            var assigneeId = WorkflowEngine.ResolveApprover(step, requester, allUsers);
            var task = ApprovalTask.Create(request.Id, step.Id, assigneeId, step.SlaHours);
            db.ApprovalTasks.Add(task);
            task.Skip();
        }

        request.Cancel();
        db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, requester.Id, AuditEventType.RequestCancelled));
    }
}
