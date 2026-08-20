using Approva.Application.Common.Interfaces;
using Approva.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Approva.Application.Requests.Queries.ListMyPendingApprovals;

public class ListMyPendingApprovalsQueryHandler
    : IRequestHandler<ListMyPendingApprovalsQuery, IReadOnlyCollection<PendingApprovalDto>>
{
    private readonly IApprovaDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ListMyPendingApprovalsQueryHandler(IApprovaDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<PendingApprovalDto>> Handle(ListMyPendingApprovalsQuery query, CancellationToken cancellationToken)
    {
        var tasks = await _db.ApprovalTasks
            .Where(t => t.AssignedToUserId == _currentUser.UserId && t.Status == ApprovalTaskStatus.Pending)
            .OrderBy(t => t.DueAt)
            .ToListAsync(cancellationToken);

        if (tasks.Count == 0)
            return [];

        var requestIds = tasks.Select(t => t.RequestId).Distinct().ToList();
        // Defense in depth: only surface tasks whose parent Request is still actually
        // Pending, in case a task was ever left Pending while its request moved on.
        var requests = await _db.Requests
            .Where(r => requestIds.Contains(r.Id) && r.TenantId == _currentUser.TenantId && r.Status == RequestStatus.Pending)
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var requesterIds = requests.Values.Select(r => r.RequesterId).Distinct().ToList();
        var requesterNames = await _db.Users.Where(u => requesterIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        var stepIds = tasks.Select(t => t.StepId).Distinct().ToList();
        var stepNames = await _db.WorkflowSteps.Where(s => stepIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        return tasks
            .Where(t => requests.ContainsKey(t.RequestId))
            .Select(t =>
            {
                var request = requests[t.RequestId];
                return new PendingApprovalDto(
                    t.Id,
                    request.Id,
                    request.Title,
                    request.Amount,
                    request.Currency,
                    requesterNames.GetValueOrDefault(request.RequesterId, "—"),
                    stepNames.GetValueOrDefault(t.StepId, "—"),
                    t.AssignedAt,
                    t.DueAt,
                    now > t.DueAt);
            })
            .ToList();
    }
}
