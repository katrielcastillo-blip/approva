using MediatR;

namespace Approva.Application.Workflows.Commands.SetWorkflowDefinitionActive;

public record SetWorkflowDefinitionActiveCommand(Guid WorkflowDefinitionId, bool IsActive) : IRequest;
