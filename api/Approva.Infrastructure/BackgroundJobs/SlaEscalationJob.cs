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

        var assigneeIds = overdue.Select(t => t.AssignedToUserId).Distinct().ToList();
        var users = await _db.Users.IgnoreQueryFilters()
            .Where(u => assigneeIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var escalatedCount = 0;

        foreach (var task in overdue)
        {
            if (!requests.TryGetValue(task.RequestId, out var request) || !users.TryGetValue(task.AssignedToUserId, out var assignee))
                continue;

            task.Escalate();

            if (assignee.ManagerId is null)
            {
                _logger.LogWarning(
                    "Tarea {TaskId} vencida pero {User} no tiene manager registrado; no se pudo escalar.",
                    task.Id, assignee.Email);
                _db.AuditEvents.Add(AuditEvent.Create(request.TenantId, request.Id, assignee.Id, AuditEventType.TaskEscalated,
                    """{"outcome":"sin_manager_para_escalar"}"""));
                continue;
            }

            var newTask = ApprovalTask.Create(task.RequestId, task.StepId, assignee.ManagerId.Value, slaHours: 24);
            _db.ApprovalTasks.Add(newTask);

            _db.AuditEvents.Add(AuditEvent.Create(request.TenantId, request.Id, assignee.Id, AuditEventType.TaskEscalated,
                $$"""{"from":"{{assignee.Name}}"}"""));

            if (users.TryGetValue(assignee.ManagerId.Value, out var manager))
            {
                await _notifications.SendAsync(manager.Email, $"Aprobación escalada: {request.Title}",
                    $"La tarea de '{assignee.Name}' para la solicitud '{request.Title}' venció su SLA y fue escalada a ti.",
                    cancellationToken);
            }

            escalatedCount++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("SlaEscalationJob: {Count} tareas escaladas de {Total} vencidas.", escalatedCount, overdue.Count);
    }
}
