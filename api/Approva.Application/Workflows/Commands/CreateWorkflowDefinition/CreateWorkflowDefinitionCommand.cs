using MediatR;

namespace Approva.Application.Workflows.Commands.CreateWorkflowDefinition;

public record CreateWorkflowConditionInput(string Field, string Operator, string Value);

public record CreateWorkflowStepInput(
    string Name,
    string ApproverType,
    string? ApproverRef,
    int SlaHours,
    string EscalationPolicy,
    List<CreateWorkflowConditionInput> Conditions);

public record CreateWorkflowDefinitionCommand(
    string Name,
    string EntityType,
    List<CreateWorkflowStepInput> Steps) : IRequest<Guid>;
