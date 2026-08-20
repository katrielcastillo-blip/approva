using Approva.Domain.Enums;
using FluentValidation;

namespace Approva.Application.Workflows.Commands.CreateWorkflowDefinition;

public class CreateWorkflowDefinitionCommandValidator : AbstractValidator<CreateWorkflowDefinitionCommand>
{
    public CreateWorkflowDefinitionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EntityType).NotEmpty().MaximumLength(100);

        RuleForEach(x => x.Steps).SetValidator(new StepValidator());
    }

    private class StepValidator : AbstractValidator<CreateWorkflowStepInput>
    {
        public StepValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ApproverType).NotEmpty()
                .Must(v => Enum.TryParse<ApproverType>(v, true, out _))
                .WithMessage($"ApproverType debe ser uno de: {string.Join(", ", Enum.GetNames<ApproverType>())}.");
            RuleFor(x => x.ApproverRef)
                .NotEmpty()
                .When(x => !string.Equals(x.ApproverType, nameof(Approva.Domain.Enums.ApproverType.Manager), StringComparison.OrdinalIgnoreCase))
                .WithMessage("ApproverRef es obligatorio salvo para ApproverType Manager.");
            RuleFor(x => x.SlaHours).GreaterThan(0);
            RuleFor(x => x.EscalationPolicy).NotEmpty()
                .Must(v => Enum.TryParse<EscalationPolicy>(v, true, out _))
                .WithMessage($"EscalationPolicy debe ser uno de: {string.Join(", ", Enum.GetNames<EscalationPolicy>())}.");

            RuleForEach(x => x.Conditions).SetValidator(new ConditionValidator());
        }
    }

    private class ConditionValidator : AbstractValidator<CreateWorkflowConditionInput>
    {
        public ConditionValidator()
        {
            RuleFor(x => x.Field).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Operator).NotEmpty()
                .Must(v => Enum.TryParse<ConditionOperator>(v, true, out _))
                .WithMessage($"Operator debe ser uno de: {string.Join(", ", Enum.GetNames<ConditionOperator>())}.");
            RuleFor(x => x.Value).NotEmpty().MaximumLength(500);
        }
    }
}
