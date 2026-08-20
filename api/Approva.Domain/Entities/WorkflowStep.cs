using Approva.Domain.Common;
using Approva.Domain.Enums;

namespace Approva.Domain.Entities;

public class WorkflowStep : Entity
{
    private readonly List<WorkflowCondition> _conditions = [];

    public Guid WorkflowDefinitionId { get; private set; }
    public int Order { get; private set; }
    public string Name { get; private set; } = null!;
    public ApproverType ApproverType { get; private set; }

    /// <summary>Meaning depends on ApproverType: Role -> User.ApproverRole to match,
    /// SpecificUser -> the target User's Id (as string), Manager -> unused.</summary>
    public string? ApproverRef { get; private set; }

    public int SlaHours { get; private set; }
    public EscalationPolicy EscalationPolicy { get; private set; }

    public IReadOnlyCollection<WorkflowCondition> Conditions => _conditions.AsReadOnly();

    private WorkflowStep()
    {
    }

    public static WorkflowStep Create(
        Guid workflowDefinitionId,
        int order,
        string name,
        ApproverType approverType,
        string? approverRef,
        int slaHours,
        EscalationPolicy escalationPolicy = EscalationPolicy.None)
    {
        if (order < 1)
            throw new DomainException("El orden del paso debe ser >= 1.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del paso es obligatorio.");
        if (approverType != ApproverType.Manager && string.IsNullOrWhiteSpace(approverRef))
            throw new DomainException("ApproverRef es obligatorio salvo para ApproverType.Manager.");
        if (slaHours <= 0)
            throw new DomainException("El SLA debe ser mayor a cero horas.");

        return new WorkflowStep
        {
            WorkflowDefinitionId = workflowDefinitionId,
            Order = order,
            Name = name.Trim(),
            ApproverType = approverType,
            ApproverRef = approverType == ApproverType.Manager ? null : approverRef!.Trim(),
            SlaHours = slaHours,
            EscalationPolicy = escalationPolicy
        };
    }

    public WorkflowCondition AddCondition(string field, ConditionOperator @operator, string value)
    {
        var condition = WorkflowCondition.Create(Id, field, @operator, value);
        _conditions.Add(condition);
        return condition;
    }

    /// <summary>AND semantics: a step with no conditions always applies.</summary>
    public bool Applies(Func<WorkflowCondition, bool> evaluate) => _conditions.Count == 0 || _conditions.All(evaluate);
}
