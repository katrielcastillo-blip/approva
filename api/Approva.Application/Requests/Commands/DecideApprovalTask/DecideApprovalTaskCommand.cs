using MediatR;

namespace Approva.Application.Requests.Commands.DecideApprovalTask;

public enum ApprovalDecision
{
    Approve,
    Reject
}

public record DecideApprovalTaskResult(Guid RequestId, string RequestStatus);

/// <summary>Decides the caller's own pending ApprovalTask for a request (POST
/// /requests/{RequestId}/decisions) — the caller doesn't need to know the task id,
/// only which request they're deciding on.</summary>
public record DecideApprovalTaskCommand(
    Guid RequestId,
    ApprovalDecision Decision,
    string? Comment) : IRequest<DecideApprovalTaskResult>;
