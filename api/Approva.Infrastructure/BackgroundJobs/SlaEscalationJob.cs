using Approva.Application.Common.Interfaces;
using Approva.Domain.Entities;
using Approva.Domain.Enums;
using Approva.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Approva.Infrastructure.BackgroundJobs;

/// <summary>Recurring Hangfire job: scans for ApprovalTasks past their SLA (DueAt) across
/// every tenant and escalates them to the assignee's manager, each with its own audited
/// task row. Runs outside any HTTP request, so it deliberately bypasses the per-tenant
/// query filter (IgnoreQueryFilters) — this is the one legitimate system-wide job.</summary>
public class SlaEscalationJob
{
    private readonly ApprovaDbContext _db;
    private readonly INotificationSender _notifications;
    private readonly ILogger<SlaEscalationJob> _logger;

    public SlaEscalationJob(ApprovaDbContext db, INotificationSender notifications, ILogger<SlaEscalationJob> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var overdue = await _db.ApprovalTasks.IgnoreQueryFilters()
            .Where(t => t.Status == ApprovalTaskStatus.Pending && t.DueAt < now)
            .ToListAsync(cancellationToken);

        if (overdue.Count == 0)
            return;

        var requestIds = overdue.Select(t => t.RequestId).Distinct().ToList();
        var requests = await _db.Requests.IgnoreQueryFilters()
            .Where(r => requestIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var assignees = await _db.Users.IgnoreQueryFilters()
            .Where(u => overdue.Select(t => t.AssignedToUserId).Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        // Also load the assignees' managers — needed both to know where to escalate to
        // and to email them. Loading only the assignees above and reusing that same
        // dictionary here would silently skip almost every manager lookup and every
        // escalation notification, since a manager is rarely also an overdue assignee.
        var managerIds = assignees.Values.Where(u => u.ManagerId.HasValue).Select(u => u.ManagerId!.Value).Distinct().ToList();
        var managers = await _db.Users.IgnoreQueryFilters()
            .Where(u => managerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var escalatedCount = 0;

        foreach (var task in overdue)
        {
            if (!requests.TryGetValue(task.RequestId, out var request) || !assignees.TryGetValue(task.AssignedToUserId, out var assignee))
                continue;

            // No manager to escalate to: deliberately leave the task Pending rather than
            // terminating it into a dead end. A previous version called task.Escalate()
            // unconditionally here, which marked the task as a terminal "Escalated" state
            // with no replacement task created — the request was left with zero Pending
            // tasks anywhere, invisible in every inbox, forever. Overdue-but-actionable
            // beats silently orphaned.
            if (assignee.ManagerId is null || !managers.TryGetValue(assignee.ManagerId.Value, out var manager))
            {
                _logger.LogWarning(
                    "Tarea {TaskId} vencida pero {User} no tiene manager registrado; se deja pendiente sin escalar.",
                    task.Id, assignee.Email);
                continue;
            }

            task.Escalate();

            var newTask = ApprovalTask.Create(task.RequestId, task.StepId, manager.Id, slaHours: 24);
            _db.ApprovalTasks.Add(newTask);

            _db.AuditEvents.Add(AuditEvent.Create(request.TenantId, request.Id, assignee.Id, AuditEventType.TaskEscalated,
                $$"""{"from":"{{assignee.Name}}","to":"{{manager.Name}}"}"""));

            await _notifications.SendAsync(manager.Email, $"Aprobación escalada: {request.Title}",
                $"La tarea de '{assignee.Name}' para la solicitud '{request.Title}' venció su SLA y fue escalada a ti.",
                cancellationToken);

            escalatedCount++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("SlaEscalationJob: {Count} tareas escaladas de {Total} vencidas.", escalatedCount, overdue.Count);
    }
}
