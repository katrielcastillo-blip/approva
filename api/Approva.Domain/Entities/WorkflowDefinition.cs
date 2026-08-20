using Approva.Domain.Common;
using Approva.Domain.Enums;

namespace Approva.Domain.Entities;

public class WorkflowDefinition : Entity
{
    private readonly List<WorkflowStep> _steps = [];

    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string EntityType { get; private set; } = null!;
    public int Version { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<WorkflowStep> Steps => _steps.AsReadOnly();

    private WorkflowDefinition()
    {
    }

    public static WorkflowDefinition Create(Guid tenantId, string name, string entityType, int version = 1)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del flujo es obligatorio.");
        if (string.IsNullOrWhiteSpace(entityType))
            throw new DomainException("El tipo de entidad del flujo es obligatorio.");

        return new WorkflowDefinition
        {
            TenantId = tenantId,
            Name = name.Trim(),
            EntityType = entityType.Trim(),
            Version = version,
            IsActive = true
        };
    }

    public WorkflowStep AddStep(string name, ApproverType approverType, string? approverRef, int slaHours,
        EscalationPolicy escalationPolicy = EscalationPolicy.None)
    {
        var nextOrder = _steps.Count == 0 ? 1 : _steps.Max(s => s.Order) + 1;
        var step = WorkflowStep.Create(Id, nextOrder, name, approverType, approverRef, slaHours, escalationPolicy);
        _steps.Add(step);
        return step;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public IEnumerable<WorkflowStep> StepsAfter(int? order) =>
        _steps.Where(s => order is null || s.Order > order).OrderBy(s => s.Order);
}
