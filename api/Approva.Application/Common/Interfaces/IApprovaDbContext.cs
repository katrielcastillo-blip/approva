using Approva.Application.Common.Models;
using Approva.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Approva.Application.Common.Interfaces;

/// <summary>Port for persistence, implemented by ApprovaDbContext in Infrastructure.
/// Keeps Application decoupled from any specific EF Core provider (Npgsql) while still
/// allowing LINQ-over-DbSet, which is the pragmatic middle ground for CQRS-lite handlers.</summary>
public interface IApprovaDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<WorkflowDefinition> WorkflowDefinitions { get; }
    DbSet<WorkflowStep> WorkflowSteps { get; }
    DbSet<WorkflowCondition> WorkflowConditions { get; }
    DbSet<Request> Requests { get; }
    DbSet<ApprovalTask> ApprovalTasks { get; }
    DbSet<AuditEvent> AuditEvents { get; }
    DbSet<IdempotencyRecord> IdempotencyRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
