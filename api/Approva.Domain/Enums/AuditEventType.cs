namespace Approva.Domain.Enums;

public enum AuditEventType
{
    RequestCreated,
    RequestSubmitted,
    TaskAssigned,
    TaskApproved,
    TaskRejected,
    TaskEscalated,
    TaskDelegated,
    RequestApproved,
    RequestRejected,
    RequestCancelled
}
