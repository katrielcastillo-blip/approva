using Approva.Domain.Common;
using Approva.Domain.Entities;
using Approva.Domain.Enums;
using Xunit;

namespace Approva.Tests.Domain;

public class ApprovalTaskTests
{
    [Fact]
    public void Create_SetsDueAtFromSla()
    {
        var before = DateTimeOffset.UtcNow;
        var task = ApprovalTask.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), slaHours: 48);
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(ApprovalTaskStatus.Pending, task.Status);
        Assert.InRange(task.DueAt, before.AddHours(48), after.AddHours(48));
    }

    [Fact]
    public void Approve_ByAssignee_Succeeds()
    {
        var assignee = Guid.NewGuid();
        var task = ApprovalTask.Create(Guid.NewGuid(), Guid.NewGuid(), assignee, 24);

        task.Approve(assignee, "se ve bien");

        Assert.Equal(ApprovalTaskStatus.Approved, task.Status);
        Assert.Equal("se ve bien", task.Comment);
        Assert.NotNull(task.DecidedAt);
    }

    [Fact]
    public void Approve_ByNonAssignee_Throws()
    {
        var task = ApprovalTask.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 24);

        Assert.Throws<DomainException>(() => task.Approve(Guid.NewGuid(), null));
    }

    [Fact]
    public void Approve_Twice_Throws()
    {
        var assignee = Guid.NewGuid();
        var task = ApprovalTask.Create(Guid.NewGuid(), Guid.NewGuid(), assignee, 24);
        task.Approve(assignee, null);

        // Simulates two concurrent approvers clicking at the same time: the second
        // decision on an already-decided task must be rejected by the domain guard.
        // (Postgres-level concurrency via RowVersion/xmin is an additional layer on
        // top of this — see the Infrastructure/EF Core configuration.)
        Assert.Throws<DomainException>(() => task.Approve(assignee, null));
    }

    [Fact]
    public void Reject_ByAssignee_Succeeds()
    {
        var assignee = Guid.NewGuid();
        var task = ApprovalTask.Create(Guid.NewGuid(), Guid.NewGuid(), assignee, 24);

        task.Reject(assignee, "monto excesivo");

        Assert.Equal(ApprovalTaskStatus.Rejected, task.Status);
    }

    [Fact]
    public void Escalate_FromPending_Succeeds()
    {
        var task = ApprovalTask.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 24);

        task.Escalate();

        Assert.Equal(ApprovalTaskStatus.Escalated, task.Status);
    }

    [Fact]
    public void Escalate_AfterDecided_Throws()
    {
        var assignee = Guid.NewGuid();
        var task = ApprovalTask.Create(Guid.NewGuid(), Guid.NewGuid(), assignee, 24);
        task.Approve(assignee, null);

        Assert.Throws<DomainException>(() => task.Escalate());
    }

    [Fact]
    public void IsOverdue_PastDueAt_ReturnsTrue()
    {
        var task = ApprovalTask.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);

        Assert.True(task.IsOverdue(DateTimeOffset.UtcNow.AddHours(2)));
        Assert.False(task.IsOverdue(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void IsOverdue_OnceDecided_ReturnsFalse()
    {
        var assignee = Guid.NewGuid();
        var task = ApprovalTask.Create(Guid.NewGuid(), Guid.NewGuid(), assignee, 1);
        task.Approve(assignee, null);

        Assert.False(task.IsOverdue(DateTimeOffset.UtcNow.AddDays(1)));
    }
}
