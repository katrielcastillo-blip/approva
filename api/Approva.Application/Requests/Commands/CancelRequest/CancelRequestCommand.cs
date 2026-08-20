using MediatR;

namespace Approva.Application.Requests.Commands.CancelRequest;

public record CancelRequestCommand(Guid RequestId) : IRequest;
