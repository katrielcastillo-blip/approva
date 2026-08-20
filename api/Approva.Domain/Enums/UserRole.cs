namespace Approva.Domain.Enums;

/// <summary>System-level permission role (RBAC), distinct from User.ApproverRole
/// which is the organizational title used for workflow routing (e.g. "CFO").</summary>
public enum UserRole
{
    Requester,
    Approver,
    Admin
}
