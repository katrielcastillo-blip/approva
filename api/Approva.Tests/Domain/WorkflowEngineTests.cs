using Approva.Domain.Common;
using Approva.Domain.Entities;
using Approva.Domain.Enums;
using Approva.Domain.Services;
using Xunit;

namespace Approva.Tests.Domain;

public class WorkflowEngineTests
{
    private static Request RequestWithAmount(decimal amount, string department = "Ops") =>
        Request.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Compra", amount, "USD",
            $$"""{"Department":"{{department}}"}""");

    [Fact]
    public void NoSteps_ReturnsNull_AutoApprove()
    {
        var definition = WorkflowDefinition.Create(Guid.NewGuid(), "Compras", "PurchaseRequest");
        var request = RequestWithAmount(100);

        var next = WorkflowEngine.DetermineNextStep(definition, request, currentStepId: null);

        Assert.Null(next);
    }

    [Fact]
    public void StepWithNoConditions_AlwaysApplies()
    {
        var definition = WorkflowDefinition.Create(Guid.NewGuid(), "Compras", "PurchaseRequest");
        definition.AddStep("Aprobación manager", ApproverType.Manager, null, slaHours: 24);
        var request = RequestWithAmount(1);

        var next = WorkflowEngine.DetermineNextStep(definition, request, currentStepId: null);

        Assert.NotNull(next);
        Assert.Equal("Aprobación manager", next!.Name);
    }

    [Fact]
    public void ConditionalStep_AppliesOnlyWhenConditionMet()
    {
        var definition = WorkflowDefinition.Create(Guid.NewGuid(), "Compras", "PurchaseRequest");
        var cfoStep = definition.AddStep("Aprobación CFO", ApproverType.Role, "CFO", slaHours: 48);
        cfoStep.AddCondition("Amount", ConditionOperator.GreaterThan, "5000");

        var cheap = RequestWithAmount(1000);
        var expensive = RequestWithAmount(10000);

        Assert.Null(WorkflowEngine.DetermineNextStep(definition, cheap, null));
        Assert.Equal(cfoStep.Id, WorkflowEngine.DetermineNextStep(definition, expensive, null)!.Id);
    }

    [Fact]
    public void MultipleConditionsOnAStep_UseAndSemantics()
    {
        var definition = WorkflowDefinition.Create(Guid.NewGuid(), "Compras", "PurchaseRequest");
        var step = definition.AddStep("Aprobación especial", ApproverType.Role, "Auditor", slaHours: 24);
        step.AddCondition("Amount", ConditionOperator.GreaterThan, "1000");
        step.AddCondition("Department", ConditionOperator.Equals, "Legal");

        var onlyAmountMatches = RequestWithAmount(5000, department: "Ops");
        var bothMatch = RequestWithAmount(5000, department: "Legal");

        Assert.Null(WorkflowEngine.DetermineNextStep(definition, onlyAmountMatches, null));
        Assert.NotNull(WorkflowEngine.DetermineNextStep(definition, bothMatch, null));
    }

    [Fact]
    public void RoutesThroughMultipleStepsInOrder_SkippingNonApplicableOnes()
    {
        // Real-world scenario from the plan's pitch: manager always approves; CFO only
        // above $5,000; CEO only above $50,000.
        var definition = WorkflowDefinition.Create(Guid.NewGuid(), "Compras", "PurchaseRequest");
        var managerStep = definition.AddStep("Manager", ApproverType.Manager, null, 24);
        var cfoStep = definition.AddStep("CFO", ApproverType.Role, "CFO", 48);
        cfoStep.AddCondition("Amount", ConditionOperator.GreaterThan, "5000");
        var ceoStep = definition.AddStep("CEO", ApproverType.Role, "CEO", 48);
        ceoStep.AddCondition("Amount", ConditionOperator.GreaterThan, "50000");

        var midRequest = RequestWithAmount(10000);

        // Step 1: manager always applies.
        var step1 = WorkflowEngine.DetermineNextStep(definition, midRequest, null);
        Assert.Equal(managerStep.Id, step1!.Id);

        // Step 2: CFO applies (10000 > 5000), CEO does not (10000 <= 50000) so it's skipped.
        var step2 = WorkflowEngine.DetermineNextStep(definition, midRequest, step1.Id);
        Assert.Equal(cfoStep.Id, step2!.Id);

        // No more applicable steps -> fully approved.
        var step3 = WorkflowEngine.DetermineNextStep(definition, midRequest, step2.Id);
        Assert.Null(step3);
    }

    [Fact]
    public void HighValueRequest_RoutesThroughAllThreeSteps()
    {
        var definition = WorkflowDefinition.Create(Guid.NewGuid(), "Compras", "PurchaseRequest");
        var managerStep = definition.AddStep("Manager", ApproverType.Manager, null, 24);
        var cfoStep = definition.AddStep("CFO", ApproverType.Role, "CFO", 48);
        cfoStep.AddCondition("Amount", ConditionOperator.GreaterThan, "5000");
        var ceoStep = definition.AddStep("CEO", ApproverType.Role, "CEO", 48);
        ceoStep.AddCondition("Amount", ConditionOperator.GreaterThan, "50000");

        var bigRequest = RequestWithAmount(100000);

        var step1 = WorkflowEngine.DetermineNextStep(definition, bigRequest, null);
        var step2 = WorkflowEngine.DetermineNextStep(definition, bigRequest, step1!.Id);
        var step3 = WorkflowEngine.DetermineNextStep(definition, bigRequest, step2!.Id);
        var step4 = WorkflowEngine.DetermineNextStep(definition, bigRequest, step3!.Id);

        Assert.Equal(managerStep.Id, step1.Id);
        Assert.Equal(cfoStep.Id, step2.Id);
        Assert.Equal(ceoStep.Id, step3.Id);
        Assert.Null(step4);
    }

    [Fact]
    public void CurrentStepNotInDefinition_Throws()
    {
        var definition = WorkflowDefinition.Create(Guid.NewGuid(), "Compras", "PurchaseRequest");
        definition.AddStep("Manager", ApproverType.Manager, null, 24);
        var request = RequestWithAmount(100);

        Assert.Throws<DomainException>(() =>
            WorkflowEngine.DetermineNextStep(definition, request, Guid.NewGuid()));
    }

    [Fact]
    public void InOperator_MatchesAnyValueInList()
    {
        var definition = WorkflowDefinition.Create(Guid.NewGuid(), "Compras", "PurchaseRequest");
        var step = definition.AddStep("Legal review", ApproverType.Role, "Legal", 24);
        step.AddCondition("Department", ConditionOperator.In, "Legal, Finance, HR");

        var matching = RequestWithAmount(1, department: "Finance");
        var nonMatching = RequestWithAmount(1, department: "Ops");

        Assert.NotNull(WorkflowEngine.DetermineNextStep(definition, matching, null));
        Assert.Null(WorkflowEngine.DetermineNextStep(definition, nonMatching, null));
    }

    // ── ResolveApprover ─────────────────────────────────────────────

    [Fact]
    public void ResolveApprover_Manager_ReturnsRequesterManagerId()
    {
        var managerId = Guid.NewGuid();
        var requester = User.Create(Guid.NewGuid(), "a@b.com", "Ana", UserRole.Requester, "hash", managerId: managerId);
        var step = WorkflowStep.Create(Guid.NewGuid(), 1, "Manager", ApproverType.Manager, null, 24);

        var resolved = WorkflowEngine.ResolveApprover(step, requester, []);

        Assert.Equal(managerId, resolved);
    }

    [Fact]
    public void ResolveApprover_Manager_WithoutManager_Throws()
    {
        var requester = User.Create(Guid.NewGuid(), "a@b.com", "Ana", UserRole.Requester, "hash");
        var step = WorkflowStep.Create(Guid.NewGuid(), 1, "Manager", ApproverType.Manager, null, 24);

        Assert.Throws<DomainException>(() => WorkflowEngine.ResolveApprover(step, requester, []));
    }

    [Fact]
    public void ResolveApprover_Role_FindsMatchingUserByApproverRole()
    {
        var tenantId = Guid.NewGuid();
        var requester = User.Create(tenantId, "a@b.com", "Ana", UserRole.Requester, "hash");
        var cfo = User.Create(tenantId, "cfo@b.com", "Carlos", UserRole.Approver, "hash", approverRole: "CFO");
        var step = WorkflowStep.Create(Guid.NewGuid(), 1, "CFO", ApproverType.Role, "CFO", 24);

        var resolved = WorkflowEngine.ResolveApprover(step, requester, [cfo]);

        Assert.Equal(cfo.Id, resolved);
    }

    [Fact]
    public void ResolveApprover_Role_NoMatch_Throws()
    {
        var tenantId = Guid.NewGuid();
        var requester = User.Create(tenantId, "a@b.com", "Ana", UserRole.Requester, "hash");
        var step = WorkflowStep.Create(Guid.NewGuid(), 1, "CFO", ApproverType.Role, "CFO", 24);

        Assert.Throws<DomainException>(() => WorkflowEngine.ResolveApprover(step, requester, []));
    }

    [Fact]
    public void ResolveApprover_SpecificUser_ReturnsThatUserId()
    {
        var requester = User.Create(Guid.NewGuid(), "a@b.com", "Ana", UserRole.Requester, "hash");
        var targetUserId = Guid.NewGuid();
        var step = WorkflowStep.Create(Guid.NewGuid(), 1, "VP", ApproverType.SpecificUser, targetUserId.ToString(), 24);

        var resolved = WorkflowEngine.ResolveApprover(step, requester, []);

        Assert.Equal(targetUserId, resolved);
    }
}
