using MediatR;

namespace Approva.Application.Users.Commands.CreateUser;

public record CreateUserCommand(
    string Email,
    string Name,
    string Password,
    string Role,
    string? ApproverRole,
    Guid? ManagerId) : IRequest<Guid>;
