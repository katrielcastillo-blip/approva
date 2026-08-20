using Approva.Application.Common.Interfaces;
using Approva.Application.Users.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Approva.Application.Users.Queries.ListUsers;

public class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, IReadOnlyCollection<UserDto>>
{
    private readonly IApprovaDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ListUsersQueryHandler(IApprovaDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<UserDto>> Handle(ListUsersQuery query, CancellationToken cancellationToken)
    {
        var users = await _db.Users.Where(u => u.TenantId == _currentUser.TenantId).ToListAsync(cancellationToken);
        var namesById = users.ToDictionary(u => u.Id, u => u.Name);

        return users.Select(u => new UserDto(
            u.Id,
            u.Email,
            u.Name,
            u.Role.ToString(),
            u.ApproverRole,
            u.ManagerId,
            u.ManagerId.HasValue ? namesById.GetValueOrDefault(u.ManagerId.Value) : null,
            u.IsOutOfOffice,
            u.DelegateUserId)).ToList();
    }
}
