namespace Approva.Application.Requests.Dtos;

public record RequestSummaryDto(
    Guid Id,
    string Title,
    decimal Amount,
    string Currency,
    string Status,
    Guid RequesterId,
    string RequesterName,
    string? CurrentStepName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
