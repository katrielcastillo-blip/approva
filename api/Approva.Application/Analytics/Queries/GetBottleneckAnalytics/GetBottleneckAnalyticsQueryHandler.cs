using Approva.Application.Common.Interfaces;
using Approva.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Approva.Application.Analytics.Queries.GetBottleneckAnalytics;

public class GetBottleneckAnalyticsQueryHandler : IRequestHandler<GetBottleneckAnalyticsQuery, BottleneckAnalyticsDto>
{
    private readonly IApprovaDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetBottleneckAnalyticsQueryHandler(IApprovaDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BottleneckAnalyticsDto> Handle(GetBottleneckAnalyticsQuery query, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var requestIds = _db.Requests.Where(r => r.TenantId == tenantId).Select(r => r.Id);

        var decided = await _db.ApprovalTasks
            .Where(t => requestIds.Contains(t.RequestId) &&
                        t.DecidedAt != null &&
                        (t.Status == ApprovalTaskStatus.Approved || t.Status == ApprovalTaskStatus.Rejected))
            .Select(t => new { t.StepId, t.AssignedAt, DecidedAt = t.DecidedAt!.Value, t.DueAt })
            .ToListAsync(cancellationToken);

        if (decided.Count == 0)
            return new BottleneckAnalyticsDto([], null);

        var stepIds = decided.Select(t => t.StepId).Distinct().ToList();
        var stepNames = await _db.WorkflowSteps.Where(s => stepIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var steps = decided
            .GroupBy(t => stepNames.GetValueOrDefault(t.StepId, "—"))
            .Select(g =>
            {
                var hours = g.Select(t => (t.DecidedAt - t.AssignedAt).TotalHours).OrderBy(h => h).ToList();
                return new StepBottleneckDto(
                    g.Key,
                    hours.Count,
                    Math.Round(hours.Average(), 1),
                    Math.Round(Median(hours), 1),
                    g.Count(t => t.DecidedAt > t.DueAt));
            })
            .OrderByDescending(s => s.AvgHoursToDecide)
            .ToList();

        return new BottleneckAnalyticsDto(steps, steps.FirstOrDefault()?.StepName);
    }

    private static double Median(IReadOnlyList<double> sortedValues)
    {
        var count = sortedValues.Count;
        if (count % 2 == 1)
            return sortedValues[count / 2];

        return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2.0;
    }
}
