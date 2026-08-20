using System.Text.Json;
using Approva.Application.Common.Interfaces;
using Approva.Application.Common.Models;
using Approva.Application.Requests.Commands.CancelRequest;
using Approva.Application.Requests.Commands.CreateRequest;
using Approva.Application.Requests.Commands.DecideApprovalTask;
using Approva.Application.Requests.Queries.GetRequestById;
using Approva.Application.Requests.Queries.ListMyPendingApprovals;
using Approva.Application.Requests.Queries.ListRequests;
using Approva.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Approva.Api.Endpoints;

public static class RequestEndpoints
{
    public static void MapRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/requests").WithTags("Requests").RequireAuthorization();

        group.MapPost("/", async (CreateRequestCommand cmd, ISender sender, CancellationToken ct) =>
        {
            var id = await sender.Send(cmd, ct);
            return Results.Created($"/requests/{id}", new { id });
        }).WithName("CreateRequest").WithSummary("Crea y envía una nueva solicitud de aprobación.");

        group.MapGet("/", async (RequestStatus? status, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListRequestsQuery(status), ct);
            return Results.Ok(result);
        }).WithName("ListRequests").WithSummary("Lista las solicitudes visibles para el usuario actual.");

        group.MapGet("/pending-approvals", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListMyPendingApprovalsQuery(), ct);
            return Results.Ok(result);
        }).WithName("ListMyPendingApprovals").WithSummary("Bandeja de aprobaciones pendientes del usuario actual.");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetRequestByIdQuery(id), ct);
            return Results.Ok(result);
        }).WithName("GetRequestById").WithSummary("Detalle de una solicitud, incluyendo tareas y auditoría.");

        group.MapPost("/{id:guid}/cancel", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new CancelRequestCommand(id), ct);
            return Results.NoContent();
        }).WithName("CancelRequest").WithSummary("Cancela una solicitud en Draft o Pending.");

        group.MapPost("/{id:guid}/decisions", HandleDecision)
            .WithName("DecideRequest")
            .WithSummary("Aprueba o rechaza la tarea pendiente del usuario actual para esta solicitud. Soporta el header Idempotency-Key.");
    }

    private record DecisionRequestBody(ApprovalDecision Decision, string? Comment);

    private static async Task<IResult> HandleDecision(
        Guid id,
        DecisionRequestBody body,
        HttpRequest httpRequest,
        ISender sender,
        IApprovaDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        var idempotencyKey = httpRequest.Headers["Idempotency-Key"].FirstOrDefault();
        var path = httpRequest.Path.ToString();

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await db.IdempotencyRecords.FirstOrDefaultAsync(
                r => r.TenantId == currentUser.TenantId && r.Key == idempotencyKey && r.RequestPath == path, ct);

            if (existing is not null)
                return Results.Json(JsonSerializer.Deserialize<object>(existing.ResponseBodyJson), statusCode: existing.ResponseStatusCode);
        }

        var result = await sender.Send(new DecideApprovalTaskCommand(id, body.Decision, body.Comment), ct);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var record = new IdempotencyRecord
            {
                Id = Guid.NewGuid(),
                TenantId = currentUser.TenantId,
                Key = idempotencyKey,
                RequestPath = path,
                ResponseStatusCode = StatusCodes.Status200OK,
                ResponseBodyJson = JsonSerializer.Serialize(result),
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.IdempotencyRecords.Add(record);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Lost the race to a concurrent identical request (unique index on
                // TenantId+Key+RequestPath) — the decision itself already committed above
                // (separate SaveChanges in the handler), so just drop the duplicate record.
            }
        }

        return Results.Ok(result);
    }
}
