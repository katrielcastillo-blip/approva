using Approva.Application.Auth.Dtos;
using Approva.Application.Common.Exceptions;
using Approva.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Approva.Application.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
    private readonly IApprovaDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public LoginCommandHandler(IApprovaDbContext db, IPasswordHasher passwordHasher, IJwtTokenGenerator tokenGenerator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResultDto> Handle(LoginCommand cmd, CancellationToken cancellationToken)
    {
        var email = cmd.Email.Trim().ToLowerInvariant();

        // Email is unique per-tenant, not globally, but login doesn't yet know the tenant
        // (that's what we're resolving here) — for the v1 demo we take the first match,
        // which is fine as long as seed/demo data doesn't reuse the same email across tenants.
        // IgnoreQueryFilters is required: the global tenant filter defaults to Guid.Empty
        // for anonymous requests, which would hide every real user.
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || user.PasswordHash is null || !_passwordHasher.Verify(cmd.Password, user.PasswordHash))
            throw new UnauthorizedException("Email o contraseña incorrectos.");

        var token = _tokenGenerator.GenerateToken(user);
        return new AuthResultDto(token, user.Id, user.TenantId, user.Email, user.Name, user.Role.ToString());
    }
}
