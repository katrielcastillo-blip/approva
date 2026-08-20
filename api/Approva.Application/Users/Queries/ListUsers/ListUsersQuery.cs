using Approva.Application.Users.Dtos;
using MediatR;

namespace Approva.Application.Users.Queries.ListUsers;

public record ListUsersQuery : IRequest<IReadOnlyCollection<UserDto>>;
