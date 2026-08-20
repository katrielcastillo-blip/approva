using MediatR;

namespace Approva.Application.Users.Commands.SetOutOfOffice;

/// <summary>A user sets their own out-of-office status; while active, any task
/// assigned to them is transparently reassigned to their DelegateUserId.</summary>
public record SetOutOfOfficeCommand(bool IsOutOfOffice, Guid? DelegateUserId) : IRequest;
