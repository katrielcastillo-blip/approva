using MediatR;

namespace Approva.Application.Analytics.Queries.GetBottleneckAnalytics;

public record StepBottleneckDto(
    string StepName,
    int DecidedTaskCount,
    double AvgHoursToDecide,
    double MedianHoursToDecide,
    int OverdueCount);

public record BottleneckAnalyticsDto(
    IReadOnlyCollection<StepBottleneckDto> Steps,
    string? SlowestStepName);

public record GetBottleneckAnalyticsQuery : IRequest<BottleneckAnalyticsDto>;
