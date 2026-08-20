using Approva.Application.Analytics.Queries.GetBottleneckAnalytics;
using MediatR;

namespace Approva.Api.Endpoints;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/analytics").WithTags("Analytics").RequireAuthorization();

        group.MapGet("/bottlenecks", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetBottleneckAnalyticsQuery(), ct);
            return Results.Ok(result);
        }).WithName("GetBottleneckAnalytics").WithSummary("Tiempo promedio/mediano por paso y el paso más lento.");
    }
}
