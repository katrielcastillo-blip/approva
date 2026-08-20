using Approva.Application.Common.Interfaces;
using Approva.Domain.Entities;
using Approva.Domain.Enums;
using Approva.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Approva.Infrastructure.Persistence;

/// <summary>Seeds a fictitious demo tenant ("Acme Corp") with users, a real 3-step
/// purchase-approval workflow, and requests spread across every status. Decisions are
/// backdated with step-specific latency (Manager fast, CFO slow, CEO medium) so the
/// bottleneck analytics dashboard shows a believable story out of the box instead of
/// "0.0h everywhere". Runs once — no-ops if any tenant exists.</summary>
public static class DbSeeder
{
    public const string DemoPassword = "Demo1234!";

    // Fixed seed so re-running against a fresh database reproduces the same demo story.
    private static readonly Random Rng = new(42);

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
        var now = DateTimeOffset.UtcNow;

        // A spread of requests across every status and every step, so the inbox,
        // request-detail audit trail, and bottleneck analytics all have real data.
        SeedApprovedRequest(db, tenant.Id, workflow, luis, allUsers, "Laptops para el equipo de diseño", 3200, "Marketing", now, daysAgo: 9);
        SeedApprovedRequest(db, tenant.Id, workflow, maria, allUsers, "Licencias de software anual", 8500, "IT", now, daysAgo: 7);
        SeedApprovedRequest(db, tenant.Id, workflow, luis, allUsers, "Renovación de flota de vehículos", 75000, "Operaciones", now, daysAgo: 12);
        SeedApprovedRequest(db, tenant.Id, workflow, maria, allUsers, "Rediseño del sitio web corporativo", 22000, "Marketing", now, daysAgo: 6);

        SeedPendingAtStep(db, tenant.Id, workflow, maria, allUsers, "Mobiliario de oficina", 1800, "Admin", stepIndex: 0, now, daysAgo: 1);
        SeedPendingAtStep(db, tenant.Id, workflow, luis, allUsers, "Consultoría de marketing digital", 12000, "Marketing", stepIndex: 1, now, daysAgo: 3);
        SeedPendingAtStep(db, tenant.Id, workflow, maria, allUsers, "Adquisición de startup local", 120000, "Estrategia", stepIndex: 2, now, daysAgo: 4);

        SeedRejectedRequest(db, tenant.Id, workflow, luis, allUsers, "Viaje de lujo a conferencia", 15000, "Ventas", now, daysAgo: 5);

        SeedCancelledRequest(db, tenant.Id, workflow, maria, allUsers, "Merchandising para evento cancelado", 2200, "Marketing", now, daysAgo: 2);

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seed completado: tenant 'acme' con {UserCount} usuarios y 9 solicitudes de ejemplo.", allUsers.Count);
    }

    /// <summary>Step-specific decision latency, in hours, so the seeded story matches the
    /// plan's own pitch: Manager decides fast, CFO is the bottleneck, CEO is in between.</summary>
    private static double LatencyHoursFor(WorkflowStep step) => step.Order switch
    {
        1 => RandomRange(2, 12),    // Manager
        2 => RandomRange(30, 100),  // CFO — the bottleneck
        _ => RandomRange(14, 50),   // CEO
    };

    private static double RandomRange(double minHours, double maxHours) => minHours + Rng.NextDouble() * (maxHours - minHours);

    private static Request NewRequest(Guid tenantId, WorkflowDefinition workflow, User requester, string title, decimal amount, string department) =>
        Request.Create(tenantId, workflow.Id, requester.Id, title, amount, "USD", $$"""{"Department":"{{department}}"}""");

    private static void SeedApprovedRequest(ApprovaDbContext db, Guid tenantId, WorkflowDefinition workflow, User requester,
        List<User> allUsers, string title, decimal amount, string department, DateTimeOffset now, int daysAgo)
    {
        var request = NewRequest(tenantId, workflow, requester, title, amount, department);
        db.Requests.Add(request);

        var clock = now.AddDays(-daysAgo);
        db.AuditEvents.Add(Backdated(AuditEvent.Create(tenantId, request.Id, requester.Id, AuditEventType.RequestCreated), clock));

        var step = WorkflowEngine.DetermineNextStep(workflow, request, null);
        request.Submit(step?.Id);

        while (step is not null)
        {
            var assigneeId = WorkflowEngine.ResolveApprover(step, requester, allUsers);
            var task = ApprovalTask.Create(request.Id, step.Id, assigneeId, step.SlaHours);
            db.ApprovalTasks.Add(task);

            var assignedAt = clock;
            var decidedAt = assignedAt.AddHours(LatencyHoursFor(step));
            task.Approve(assigneeId, "Aprobado.");
            task.BackdateForSeed(assignedAt, assignedAt.AddHours(step.SlaHours), decidedAt);
            db.AuditEvents.Add(Backdated(AuditEvent.Create(tenantId, request.Id, assigneeId, AuditEventType.TaskApproved), decidedAt));
            clock = decidedAt;

            step = WorkflowEngine.DetermineNextStep(workflow, request, step.Id);
            request.AdvanceTo(step?.Id);
        }

        db.AuditEvents.Add(Backdated(AuditEvent.Create(tenantId, request.Id, requester.Id, AuditEventType.RequestApproved), clock));
        request.BackdateForSeed(now.AddDays(-daysAgo), clock);
    }

    private static void SeedPendingAtStep(ApprovaDbContext db, Guid tenantId, WorkflowDefinition workflow, User requester,
        List<User> allUsers, string title, decimal amount, string department, int stepIndex, DateTimeOffset now, int daysAgo)
    {
        var request = NewRequest(tenantId, workflow, requester, title, amount, department);
        db.Requests.Add(request);

        var clock = now.AddDays(-daysAgo);
        db.AuditEvents.Add(Backdated(AuditEvent.Create(tenantId, request.Id, requester.Id, AuditEventType.RequestCreated), clock));

        var step = WorkflowEngine.DetermineNextStep(workflow, request, null);
        request.Submit(step?.Id);

        for (var i = 0; i < stepIndex && step is not null; i++)
        {
            var assigneeId = WorkflowEngine.ResolveApprover(step, requester, allUsers);
            var task = ApprovalTask.Create(request.Id, step.Id, assigneeId, step.SlaHours);
            db.ApprovalTasks.Add(task);

            var assignedAt = clock;
            var decidedAt = assignedAt.AddHours(LatencyHoursFor(step));
            task.Approve(assigneeId, "Aprobado.");
            task.BackdateForSeed(assignedAt, assignedAt.AddHours(step.SlaHours), decidedAt);
            db.AuditEvents.Add(Backdated(AuditEvent.Create(tenantId, request.Id, assigneeId, AuditEventType.TaskApproved), decidedAt));
            clock = decidedAt;

            step = WorkflowEngine.DetermineNextStep(workflow, request, step.Id);
            request.AdvanceTo(step?.Id);
        }

        if (step is not null)
        {
            var assigneeId = WorkflowEngine.ResolveApprover(step, requester, allUsers);
            var pendingTask = ApprovalTask.Create(request.Id, step.Id, assigneeId, step.SlaHours);
            db.ApprovalTasks.Add(pendingTask);
            pendingTask.BackdateForSeed(clock, clock.AddHours(step.SlaHours), null);
            db.AuditEvents.Add(Backdated(AuditEvent.Create(tenantId, request.Id, requester.Id, AuditEventType.TaskAssigned), clock));
        }

        request.BackdateForSeed(now.AddDays(-daysAgo), null);
    }

    private static void SeedRejectedRequest(ApprovaDbContext db, Guid tenantId, WorkflowDefinition workflow, User requester,
        List<User> allUsers, string title, decimal amount, string department, DateTimeOffset now, int daysAgo)
    {
        var request = NewRequest(tenantId, workflow, requester, title, amount, department);
        db.Requests.Add(request);

        var clock = now.AddDays(-daysAgo);
        db.AuditEvents.Add(Backdated(AuditEvent.Create(tenantId, request.Id, requester.Id, AuditEventType.RequestCreated), clock));

        var step = WorkflowEngine.DetermineNextStep(workflow, request, null)!;
        request.Submit(step.Id);

        var assigneeId = WorkflowEngine.ResolveApprover(step, requester, allUsers);
        var task = ApprovalTask.Create(request.Id, step.Id, assigneeId, step.SlaHours);
        db.ApprovalTasks.Add(task);

        var decidedAt = clock.AddHours(LatencyHoursFor(step));
        task.Reject(assigneeId, "Fuera de política de gastos.");
        task.BackdateForSeed(clock, clock.AddHours(step.SlaHours), decidedAt);
        db.AuditEvents.Add(Backdated(AuditEvent.Create(tenantId, request.Id, assigneeId, AuditEventType.TaskRejected), decidedAt));

        request.Reject();
        db.AuditEvents.Add(Backdated(AuditEvent.Create(tenantId, request.Id, assigneeId, AuditEventType.RequestRejected), decidedAt));
        request.BackdateForSeed(now.AddDays(-daysAgo), decidedAt);
    }

    private static void SeedCancelledRequest(ApprovaDbContext db, Guid tenantId, WorkflowDefinition workflow, User requester,
        List<User> allUsers, string title, decimal amount, string department, DateTimeOffset now, int daysAgo)
    {
        var request = NewRequest(tenantId, workflow, requester, title, amount, department);
        db.Requests.Add(request);

        var clock = now.AddDays(-daysAgo);
        db.AuditEvents.Add(Backdated(AuditEvent.Create(tenantId, request.Id, requester.Id, AuditEventType.RequestCreated), clock));

        var step = WorkflowEngine.DetermineNextStep(workflow, request, null);
        request.Submit(step?.Id);

        if (step is not null)
        {
            var assigneeId = WorkflowEngine.ResolveApprover(step, requester, allUsers);
            var task = ApprovalTask.Create(request.Id, step.Id, assigneeId, step.SlaHours);
            db.ApprovalTasks.Add(task);
            task.Skip();
            task.BackdateForSeed(clock, clock.AddHours(step.SlaHours), clock.AddHours(1));
        }

        var completedAt = clock.AddHours(1);
        request.Cancel();
        db.AuditEvents.Add(Backdated(AuditEvent.Create(tenantId, request.Id, requester.Id, AuditEventType.RequestCancelled), completedAt));
        request.BackdateForSeed(now.AddDays(-daysAgo), completedAt);
    }

    private static AuditEvent Backdated(AuditEvent auditEvent, DateTimeOffset occurredAt)
    {
        auditEvent.BackdateForSeed(occurredAt);
        return auditEvent;
    }
}
