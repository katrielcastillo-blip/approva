using Approva.Application.Common.Exceptions;
using Approva.Application.Common.Interfaces;
using Approva.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Approva.Application.Users.Commands.SetOutOfOffice;

public class SetOutOfOfficeCommandHandler : IRequestHandler<SetOutOfOfficeCommand>
{
    private readonly IApprovaDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SetOutOfOfficeCommandHandler(IApprovaDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(SetOutOfOfficeCommand cmd, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), _currentUser.UserId);

        user.SetOutOfOffice(cmd.IsOutOfOffice, cmd.DelegateUserId);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
