using Approva.Application.Common.Interfaces;
using Approva.Application.Requests.Dtos;
using Approva.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Approva.Application.Requests.Queries.ListRequests;

public class ListRequestsQueryHandler : IRequestHandler<ListRequestsQuery, IReadOnlyCollection<RequestSummaryDto>>
{
    private readonly IApprovaDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ListRequestsQueryHandler(IApprovaDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<RequestSummaryDto>> Handle(ListRequestsQuery query, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        var requestsQuery = _db.Requests.Where(r => r.TenantId == tenantId);

        if (_currentUser.Role != UserRole.Admin)
            requestsQuery = requestsQuery.Where(r => r.RequesterId == _currentUser.UserId);

        if (query.Status is not null)
            requestsQuery = requestsQuery.Where(r => r.Status == query.Status);

        var requests = await requestsQuery.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);

        var requesterIds = requests.Select(r => r.RequesterId).Distinct().ToList();
        var requesters = await _db.Users.Where(u => requesterIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        var stepIds = requests.Where(r => r.CurrentStepId.HasValue).Select(r => r.CurrentStepId!.Value).Distinct().ToList();
        var stepNames = await _db.WorkflowSteps.Where(s => stepIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        return requests.Select(r => new RequestSummaryDto(
            r.Id,
            r.Title,
            r.Amount,
            r.Currency,
            r.Status.ToString(),
            r.RequesterId,
            requesters.GetValueOrDefault(r.RequesterId, "—"),
            r.CurrentStepId.HasValue ? stepNames.GetValueOrDefault(r.CurrentStepId.Value) : null,
            r.CreatedAt,
            r.CompletedAt)).ToList();
    }
}
