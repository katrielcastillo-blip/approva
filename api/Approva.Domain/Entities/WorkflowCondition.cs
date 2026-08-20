using Approva.Domain.Common;
using Approva.Domain.Enums;

namespace Approva.Domain.Entities;

public class WorkflowCondition : Entity
{
    public Guid WorkflowStepId { get; private set; }
    public string Field { get; private set; } = null!;
    public ConditionOperator Operator { get; private set; }

    /// <summary>Raw comparison value. For In/NotIn, a comma-separated list.</summary>
    public string Value { get; private set; } = null!;

    private WorkflowCondition()
    {
    }

    public static WorkflowCondition Create(Guid workflowStepId, string field, ConditionOperator @operator, string value)
    {
        if (string.IsNullOrWhiteSpace(field))
            throw new DomainException("El campo de la condición es obligatorio.");
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("El valor de la condición es obligatorio.");

        return new WorkflowCondition
        {
            WorkflowStepId = workflowStepId,
            Field = field.Trim(),
            Operator = @operator,
            Value = value.Trim()
        };
    }
}
