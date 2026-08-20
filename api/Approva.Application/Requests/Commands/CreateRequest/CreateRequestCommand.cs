using MediatR;

namespace Approva.Application.Requests.Commands.CreateRequest;

public record CreateRequestCommand(
    Guid WorkflowDefinitionId,
    string Title,
    decimal Amount,
    string Currency,
    string PayloadJson) : IRequest<Guid>;
