using Approva.Domain.Enums;
using FluentValidation;

namespace Approva.Application.Users.Commands.CreateUser;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Role).NotEmpty()
            .Must(v => Enum.TryParse<UserRole>(v, true, out _))
            .WithMessage($"Role debe ser uno de: {string.Join(", ", Enum.GetNames<UserRole>())}.");
        RuleFor(x => x.ApproverRole).MaximumLength(100);
    }
}
