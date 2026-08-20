namespace Approva.Application.Auth.Dtos;

public record AuthResultDto(
    string Token,
    Guid UserId,
    Guid TenantId,
    string Email,
    string Name,
    string Role);
