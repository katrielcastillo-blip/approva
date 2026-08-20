using Approva.Application.Workflows.Commands.CreateWorkflowDefinition;
using Approva.Application.Workflows.Commands.SetWorkflowDefinitionActive;
using Approva.Application.Workflows.Queries.GetWorkflowDefinitionById;
using Approva.Application.Workflows.Queries.ListWorkflowDefinitions;
using MediatR;

namespace Approva.Api.Endpoints;

public static class WorkflowEndpoints
{
    public static void MapWorkflowEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/workflow-definitions")
            .WithTags("Workflows")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/", async (CreateWorkflowDefinitionCommand cmd, ISender sender, CancellationToken ct) =>
        {
            var id = await sender.Send(cmd, ct);
            return Results.Created($"/workflow-definitions/{id}", new { id });
        }).WithName("CreateWorkflowDefinition").WithSummary("Define un nuevo flujo de aprobación configurable.");

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListWorkflowDefinitionsQuery(), ct);
            return Results.Ok(result);
        }).WithName("ListWorkflowDefinitions").WithSummary("Lista los flujos definidos para el tenant.");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetWorkflowDefinitionByIdQuery(id), ct);
            return Results.Ok(result);
        }).WithName("GetWorkflowDefinitionById").WithSummary("Detalle de un flujo con sus pasos y condiciones.");

        group.MapPost("/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new SetWorkflowDefinitionActiveCommand(id, true), ct);
            return Results.NoContent();
        }).WithName("ActivateWorkflowDefinition");

        group.MapPost("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new SetWorkflowDefinitionActiveCommand(id, false), ct);
            return Results.NoContent();
        }).WithName("DeactivateWorkflowDefinition");
    }
}
