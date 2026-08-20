using MediatR;

namespace Approva.Application.Requests.Queries.ListMyPendingApprovals;

public record PendingApprovalDto(
    Guid TaskId,
    Guid RequestId,
    string RequestTitle,
    decimal Amount,
    string Currency,
    string RequesterName,
    string StepName,
    DateTimeOffset AssignedAt,
    DateTimeOffset DueAt,
    bool IsOverdue);

public record ListMyPendingApprovalsQuery : IRequest<IReadOnlyCollection<PendingApprovalDto>>;
