using Approva.Application.Auth.Dtos;
using Approva.Application.Common.Exceptions;
using Approva.Application.Common.Interfaces;
using Approva.Domain.Entities;
using Approva.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Approva.Application.Auth.Commands.RegisterTenant;

public class RegisterTenantCommandHandler : IRequestHandler<RegisterTenantCommand, AuthResultDto>
{
    private readonly IApprovaDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public RegisterTenantCommandHandler(IApprovaDbContext db, IPasswordHasher passwordHasher, IJwtTokenGenerator tokenGenerator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResultDto> Handle(RegisterTenantCommand cmd, CancellationToken cancellationToken)
    {
        var slugTaken = await _db.Tenants.AnyAsync(t => t.Slug == cmd.TenantSlug.ToLowerInvariant(), cancellationToken);
        if (slugTaken)
            throw new ConflictException($"El slug '{cmd.TenantSlug}' ya está en uso.");

        var tenant = Tenant.Create(cmd.TenantName, cmd.TenantSlug);
        _db.Tenants.Add(tenant);

        var admin = User.Create(
            tenant.Id, cmd.AdminEmail, cmd.AdminName, UserRole.Admin, _passwordHasher.Hash(cmd.AdminPassword));
        _db.Users.Add(admin);

        await _db.SaveChangesAsync(cancellationToken);

        var token = _tokenGenerator.GenerateToken(admin);
        return new AuthResultDto(token, admin.Id, tenant.Id, admin.Email, admin.Name, admin.Role.ToString());
    }
}
