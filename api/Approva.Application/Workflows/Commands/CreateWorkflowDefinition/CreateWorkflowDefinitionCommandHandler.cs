using Approva.Application.Common.Interfaces;
using Approva.Domain.Entities;
using Approva.Domain.Enums;
using MediatR;

namespace Approva.Application.Workflows.Commands.CreateWorkflowDefinition;

public class CreateWorkflowDefinitionCommandHandler : IRequestHandler<CreateWorkflowDefinitionCommand, Guid>
{
    private readonly IApprovaDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateWorkflowDefinitionCommandHandler(IApprovaDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateWorkflowDefinitionCommand cmd, CancellationToken cancellationToken)
    {
        var definition = WorkflowDefinition.Create(_currentUser.TenantId, cmd.Name, cmd.EntityType);

        foreach (var stepInput in cmd.Steps)
        {
            var approverType = Enum.Parse<ApproverType>(stepInput.ApproverType, true);
            var escalationPolicy = Enum.Parse<EscalationPolicy>(stepInput.EscalationPolicy, true);

            var step = definition.AddStep(stepInput.Name, approverType, stepInput.ApproverRef, stepInput.SlaHours, escalationPolicy);

            foreach (var conditionInput in stepInput.Conditions)
            {
                var op = Enum.Parse<ConditionOperator>(conditionInput.Operator, true);
                step.AddCondition(conditionInput.Field, op, conditionInput.Value);
            }
        }

        _db.WorkflowDefinitions.Add(definition);
        await _db.SaveChangesAsync(cancellationToken);

        return definition.Id;
    }
}
