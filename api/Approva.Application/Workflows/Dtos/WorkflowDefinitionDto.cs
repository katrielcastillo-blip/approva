namespace Approva.Application.Workflows.Dtos;

public record WorkflowConditionDto(Guid Id, string Field, string Operator, string Value);

public record WorkflowStepDto(
    Guid Id,
    int Order,
    string Name,
    string ApproverType,
    string? ApproverRef,
    int SlaHours,
    string EscalationPolicy,
    IReadOnlyCollection<WorkflowConditionDto> Conditions);

public record WorkflowDefinitionDto(
    Guid Id,
    string Name,
    string EntityType,
    int Version,
    bool IsActive,
    IReadOnlyCollection<WorkflowStepDto> Steps);

public record WorkflowDefinitionSummaryDto(
    Guid Id,
    string Name,
    string EntityType,
    int Version,
    bool IsActive,
    int StepCount);
