using Approva.Application.Auth.Commands.Login;
using Approva.Application.Auth.Commands.RegisterTenant;
using MediatR;

namespace Approva.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register-tenant", async (RegisterTenantCommand cmd, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(cmd, ct);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("RegisterTenant")
        .WithSummary("Crea un nuevo tenant con su primer usuario administrador y devuelve un JWT.");

        group.MapPost("/login", async (LoginCommand cmd, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(cmd, ct);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("Login")
        .WithSummary("Autentica un usuario y devuelve un JWT.");
    }
}
