using FluentValidation;

namespace Approva.Application.Requests.Commands.DecideApprovalTask;

public class DecideApprovalTaskCommandValidator : AbstractValidator<DecideApprovalTaskCommand>
{
    public DecideApprovalTaskCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.Decision).IsInEnum();
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}
