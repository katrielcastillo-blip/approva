using Approva.Application.Common.Exceptions;
using Approva.Application.Common.Interfaces;
using Approva.Domain.Entities;
using Approva.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Approva.Application.Users.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IApprovaDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserCommandHandler(IApprovaDbContext db, ICurrentUserService currentUser, IPasswordHasher passwordHasher)
    {
        _db = db;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(CreateUserCommand cmd, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var email = cmd.Email.Trim().ToLowerInvariant();

        var emailTaken = await _db.Users.AnyAsync(u => u.TenantId == tenantId && u.Email == email, cancellationToken);
        if (emailTaken)
            throw new ConflictException($"Ya existe un usuario con el email '{email}' en este tenant.");

        if (cmd.ManagerId is not null)
        {
            var managerExists = await _db.Users.AnyAsync(u => u.Id == cmd.ManagerId && u.TenantId == tenantId, cancellationToken);
            if (!managerExists)
                throw new NotFoundException(nameof(User), cmd.ManagerId);
        }

        var role = Enum.Parse<UserRole>(cmd.Role, true);
        var user = User.Create(tenantId, email, cmd.Name, role, _passwordHasher.Hash(cmd.Password), cmd.ApproverRole, cmd.ManagerId);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
