namespace Approva.Domain.Enums;

/// <summary>
/// How a WorkflowStep resolves who the approver is.
/// Role: any user with ApproverRef as their Role. SpecificUser: ApproverRef is a User Id.
/// Manager: the requester's direct manager (ApproverRef unused).
/// </summary>
public enum ApproverType
{
    Role,
    SpecificUser,
    Manager
}
