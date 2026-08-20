namespace Approva.Domain.Enums;

/// <summary>What happens when an ApprovalTask's SLA (DueAt) is breached.</summary>
public enum EscalationPolicy
{
    None,
    EscalateToManager
}
