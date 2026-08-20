namespace Approva.Application.Users.Dtos;

public record UserDto(
    Guid Id,
    string Email,
    string Name,
    string Role,
    string? ApproverRole,
    Guid? ManagerId,
    string? ManagerName,
    bool IsOutOfOffice,
    Guid? DelegateUserId);
