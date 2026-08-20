using Approva.Domain.Common;
using Approva.Domain.Enums;

namespace Approva.Domain.Entities;

public class ApprovalTask : Entity
{
    public Guid RequestId { get; private set; }
    public Guid StepId { get; private set; }
    public Guid AssignedToUserId { get; private set; }
    public ApprovalTaskStatus Status { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public DateTimeOffset DueAt { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public string? Comment { get; private set; }

    /// <summary>Optimistic concurrency token, mapped to Postgres' xmin system column.</summary>
    public uint RowVersion { get; private set; }

    private ApprovalTask()
    {
    }

    public static ApprovalTask Create(Guid requestId, Guid stepId, Guid assignedToUserId, int slaHours)
    {
        if (slaHours <= 0)
            throw new DomainException("El SLA debe ser mayor a cero horas.");

        var now = DateTimeOffset.UtcNow;
        return new ApprovalTask
        {
            RequestId = requestId,
            StepId = stepId,
            AssignedToUserId = assignedToUserId,
            Status = ApprovalTaskStatus.Pending,
            AssignedAt = now,
            DueAt = now.AddHours(slaHours)
        };
    }

    public void Approve(Guid actorId, string? comment)
    {
        EnsurePending();
        EnsureActorIsAssignee(actorId);

        Status = ApprovalTaskStatus.Approved;
        Comment = comment;
        DecidedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(Guid actorId, string? comment)
    {
        EnsurePending();
        EnsureActorIsAssignee(actorId);

        Status = ApprovalTaskStatus.Rejected;
        Comment = comment;
        DecidedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Terminates this task because its SLA was breached. The caller is responsible
    /// for creating a new ApprovalTask for the same step assigned to the escalation target
    /// (typically the assignee's manager) — each reassignment gets its own audited task row.</summary>
    public void Escalate()
    {
        EnsurePending();

        Status = ApprovalTaskStatus.Escalated;
        DecidedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Terminates this task because the assignee is out-of-office. The caller is
    /// responsible for creating a new ApprovalTask assigned to the registered delegate.</summary>
    public void Delegate()
    {
        EnsurePending();

        Status = ApprovalTaskStatus.Delegated;
        DecidedAt = DateTimeOffset.UtcNow;
    }

    public void Skip()
    {
        EnsurePending();

        Status = ApprovalTaskStatus.Skipped;
        DecidedAt = DateTimeOffset.UtcNow;
    }

    public bool IsOverdue(DateTimeOffset asOf) => Status == ApprovalTaskStatus.Pending && asOf > DueAt;

    private void EnsurePending()
    {
        if (Status != ApprovalTaskStatus.Pending)
            throw new DomainException($"La tarea ya fue decidida (estado: {Status}).");
    }

    private void EnsureActorIsAssignee(Guid actorId)
    {
        if (actorId != AssignedToUserId)
            throw new DomainException("Solo el usuario asignado puede decidir esta tarea.");
    }
}
