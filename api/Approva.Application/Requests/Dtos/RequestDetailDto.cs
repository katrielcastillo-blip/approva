namespace Approva.Application.Requests.Dtos;

public record RequestDetailDto(
    Guid Id,
    string Title,
    decimal Amount,
    string Currency,
    string PayloadJson,
    string Status,
    Guid RequesterId,
    string RequesterName,
    Guid WorkflowDefinitionId,
    string WorkflowDefinitionName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyCollection<ApprovalTaskDto> Tasks,
    IReadOnlyCollection<AuditEventDto> AuditTrail);

public record ApprovalTaskDto(
    Guid Id,
    string StepName,
    Guid AssignedToUserId,
    string AssignedToUserName,
    string Status,
    DateTimeOffset AssignedAt,
    DateTimeOffset DueAt,
    DateTimeOffset? DecidedAt,
    string? Comment);

public record AuditEventDto(
    Guid Id,
    string EventType,
    Guid ActorId,
    string ActorName,
    string PayloadJson,
    DateTimeOffset OccurredAt);
