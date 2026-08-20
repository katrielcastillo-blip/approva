using Approva.Domain.Common;
using Approva.Domain.Entities;
using Approva.Domain.Enums;
using Xunit;

namespace Approva.Tests.Domain;

public class RequestTests
{
    private static Request NewDraft(decimal amount = 100) =>
        Request.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Compra de laptops", amount, "USD");

    [Fact]
    public void Create_StartsInDraft()
    {
        var request = NewDraft();

        Assert.Equal(RequestStatus.Draft, request.Status);
        Assert.Null(request.CurrentStepId);
        Assert.Null(request.CompletedAt);
    }

    [Fact]
    public void Create_WithNegativeAmount_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Request.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x", -1, "USD"));
    }

    [Fact]
    public void Create_WithInvalidPayloadJson_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Request.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x", 1, "USD", "{not-json"));
    }

    [Fact]
    public void Submit_WithAStep_MovesToPending()
    {
        var request = NewDraft();
        var stepId = Guid.NewGuid();

        request.Submit(stepId);

        Assert.Equal(RequestStatus.Pending, request.Status);
        Assert.Equal(stepId, request.CurrentStepId);
        Assert.Null(request.CompletedAt);
    }

    [Fact]
    public void Submit_WithNoApplicableStep_AutoApproves()
    {
        var request = NewDraft();

        request.Submit(null);

        Assert.Equal(RequestStatus.Approved, request.Status);
        Assert.Null(request.CurrentStepId);
        Assert.NotNull(request.CompletedAt);
    }

    [Fact]
    public void Submit_Twice_Throws()
    {
        var request = NewDraft();
        request.Submit(Guid.NewGuid());

        Assert.Throws<DomainException>(() => request.Submit(Guid.NewGuid()));
    }

    [Fact]
    public void AdvanceTo_NextStep_StaysPending()
    {
        var request = NewDraft();
        request.Submit(Guid.NewGuid());
        var nextStepId = Guid.NewGuid();

        request.AdvanceTo(nextStepId);

        Assert.Equal(RequestStatus.Pending, request.Status);
        Assert.Equal(nextStepId, request.CurrentStepId);
    }

    [Fact]
    public void AdvanceTo_Null_Approves()
    {
        var request = NewDraft();
        request.Submit(Guid.NewGuid());

        request.AdvanceTo(null);

        Assert.Equal(RequestStatus.Approved, request.Status);
        Assert.NotNull(request.CompletedAt);
    }

    [Fact]
    public void AdvanceTo_FromDraft_Throws()
    {
        var request = NewDraft();

        Assert.Throws<DomainException>(() => request.AdvanceTo(Guid.NewGuid()));
    }

    [Fact]
    public void Reject_FromPending_Succeeds()
    {
        var request = NewDraft();
        request.Submit(Guid.NewGuid());

        request.Reject();

        Assert.Equal(RequestStatus.Rejected, request.Status);
        Assert.Null(request.CurrentStepId);
        Assert.NotNull(request.CompletedAt);
    }

    [Fact]
    public void Reject_FromDraft_Throws()
    {
        var request = NewDraft();

        Assert.Throws<DomainException>(() => request.Reject());
    }

    [Fact]
    public void ApprovedRequest_RejectsFurtherDecisions()
    {
        var request = NewDraft();
        request.Submit(null); // auto-approved, no steps

        Assert.Throws<DomainException>(() => request.Reject());
        Assert.Throws<DomainException>(() => request.AdvanceTo(Guid.NewGuid()));
    }

    [Theory]
    [InlineData(RequestStatus.Draft)]
    [InlineData(RequestStatus.Pending)]
    public void Cancel_FromCancellableStates_Succeeds(RequestStatus status)
    {
        var request = NewDraft();
        if (status == RequestStatus.Pending)
            request.Submit(Guid.NewGuid());

        request.Cancel();

        Assert.Equal(RequestStatus.Cancelled, request.Status);
    }

    [Fact]
    public void Cancel_AfterApproved_Throws()
    {
        var request = NewDraft();
        request.Submit(null);

        Assert.Throws<DomainException>(() => request.Cancel());
    }

    [Fact]
    public void GetFieldValue_ResolvesWellKnownFieldsAndPayload()
    {
        var request = Request.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x", 5000, "usd",
            """{"Department":"Finance"}""");

        Assert.Equal("5000", request.GetFieldValue("Amount"));
        Assert.Equal("USD", request.GetFieldValue("Currency"));
        Assert.Equal("Finance", request.GetFieldValue("Department"));
        Assert.Null(request.GetFieldValue("DoesNotExist"));
    }
}
