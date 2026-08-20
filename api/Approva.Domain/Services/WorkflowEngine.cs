using Approva.Domain.Common;
using Approva.Domain.Entities;
using Approva.Domain.Enums;

namespace Approva.Domain.Services;

/// <summary>The rules engine: given a Request and the WorkflowDefinition that governs it,
/// decides what the next step is. Pure in-memory logic — no database, no I/O — so it can
/// (and must) be unit tested without any infrastructure.</summary>
public static class WorkflowEngine
{
    /// <summary>Walks the definition's steps in order starting after <paramref name="currentStepId"/>
    /// (null means "from the start") and returns the first step whose conditions all evaluate
    /// true (AND semantics; a step with no conditions always applies). Null means no further
    /// step applies — the request is fully approved.</summary>
    public static WorkflowStep? DetermineNextStep(WorkflowDefinition definition, Request request, Guid? currentStepId)
    {
        int? currentOrder = null;
        if (currentStepId is not null)
        {
            var currentStep = definition.Steps.FirstOrDefault(s => s.Id == currentStepId);
            if (currentStep is null)
                throw new DomainException("El paso actual no pertenece a esta definición de flujo.");
            currentOrder = currentStep.Order;
        }

        foreach (var step in definition.StepsAfter(currentOrder))
        {
            if (step.Applies(condition => ConditionEvaluator.Evaluate(condition, request)))
                return step;
        }

        return null;
    }

    /// <summary>Resolves which user should be assigned the ApprovalTask for a given step.
    /// Purely a lookup over data already fetched by the caller — no DB access here.</summary>
    public static Guid ResolveApprover(WorkflowStep step, User requester, IReadOnlyCollection<User> tenantUsers)
    {
        switch (step.ApproverType)
        {
            case ApproverType.Manager:
                return requester.ManagerId
                    ?? throw new DomainException(
                        $"El solicitante no tiene manager asignado; el paso '{step.Name}' no se puede resolver.");

            case ApproverType.SpecificUser:
                return Guid.Parse(step.ApproverRef!);

            case ApproverType.Role:
                var approver = tenantUsers.FirstOrDefault(u =>
                    string.Equals(u.ApproverRole, step.ApproverRef, StringComparison.OrdinalIgnoreCase));
                return approver?.Id
                    ?? throw new DomainException(
                        $"No se encontró un usuario con rol de aprobador '{step.ApproverRef}' para el paso '{step.Name}'.");

            default:
                throw new ArgumentOutOfRangeException(nameof(step), step.ApproverType, "ApproverType no soportado.");
        }
    }
}
