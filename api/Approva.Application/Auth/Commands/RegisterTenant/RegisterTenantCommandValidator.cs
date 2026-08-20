using FluentValidation;

namespace Approva.Application.Auth.Commands.RegisterTenant;

public class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand>
{
    public RegisterTenantCommandValidator()
    {
        RuleFor(x => x.TenantName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TenantSlug).NotEmpty().MaximumLength(100)
            .Matches("^[a-z0-9-]+$").WithMessage("El slug solo puede contener minúsculas, números y guiones.");
        RuleFor(x => x.AdminName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.AdminPassword).NotEmpty().MinimumLength(8);
    }
}
