using Approva.Application.Workflows.Dtos;
using MediatR;

namespace Approva.Application.Workflows.Queries.GetWorkflowDefinitionById;

public record GetWorkflowDefinitionByIdQuery(Guid WorkflowDefinitionId) : IRequest<WorkflowDefinitionDto>;
