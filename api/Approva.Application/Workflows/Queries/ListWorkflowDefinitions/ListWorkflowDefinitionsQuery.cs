using Approva.Application.Workflows.Dtos;
using MediatR;

namespace Approva.Application.Workflows.Queries.ListWorkflowDefinitions;

public record ListWorkflowDefinitionsQuery : IRequest<IReadOnlyCollection<WorkflowDefinitionSummaryDto>>;
