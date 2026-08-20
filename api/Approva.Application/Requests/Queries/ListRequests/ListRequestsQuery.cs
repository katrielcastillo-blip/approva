using Approva.Application.Requests.Dtos;
using Approva.Domain.Enums;
using MediatR;

namespace Approva.Application.Requests.Queries.ListRequests;

/// <summary>Lists requests visible to the current user: Admins see the whole tenant,
/// everyone else sees only requests they created (their "My Requests" view).
/// Use ListMyPendingApprovalsQuery for the approver's inbox instead.</summary>
public record ListRequestsQuery(RequestStatus? Status = null) : IRequest<IReadOnlyCollection<RequestSummaryDto>>;
