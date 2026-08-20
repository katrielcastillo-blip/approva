using Approva.Application.Auth.Dtos;
using MediatR;

namespace Approva.Application.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<AuthResultDto>;
