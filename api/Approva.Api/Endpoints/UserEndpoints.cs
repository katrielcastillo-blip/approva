using Approva.Application.Users.Commands.CreateUser;
using Approva.Application.Users.Commands.SetOutOfOffice;
using Approva.Application.Users.Queries.ListUsers;
using MediatR;

namespace Approva.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users").WithTags("Users").RequireAuthorization();

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListUsersQuery(), ct);
            return Results.Ok(result);
        }).WithName("ListUsers").WithSummary("Lista los usuarios del tenant.");

        group.MapPost("/", async (CreateUserCommand cmd, ISender sender, CancellationToken ct) =>
        {
            var id = await sender.Send(cmd, ct);
            return Results.Created($"/users/{id}", new { id });
        }).WithName("CreateUser").RequireAuthorization("AdminOnly").WithSummary("Crea un usuario en el tenant (solo Admin).");

        group.MapPost("/me/out-of-office", async (SetOutOfOfficeCommand cmd, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(cmd, ct);
            return Results.NoContent();
        }).WithName("SetOutOfOffice").WithSummary("Activa o desactiva el modo fuera de oficina con un delegado.");
    }
}
