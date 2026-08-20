using Approva.Application.Common.Interfaces;
using Approva.Application.Common.Models;
using Approva.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Approva.Infrastructure.Persistence;

public class ApprovaDbContext : DbContext, IApprovaDbContext
{
    private readonly ICurrentUserService? _currentUser;

    public ApprovaDbContext(DbContextOptions<ApprovaDbContext> options, ICurrentUserService? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<WorkflowCondition> WorkflowConditions => Set<WorkflowCondition>();
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<ApprovalTask> ApprovalTasks => Set<ApprovalTask>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApprovaDbContext).Assembly);

        // Defense-in-depth multi-tenant isolation: every read against these tables is
        // implicitly scoped to the caller's tenant, even if a handler forgets an explicit
        // TenantId filter. Cross-tenant reads that are legitimately needed (e.g. Login,
        // which resolves the tenant from an email) must opt out with IgnoreQueryFilters().
        // _currentUser is a captured field, so EF Core re-evaluates it on every query.
        modelBuilder.Entity<User>().HasQueryFilter(u => _currentUser == null || u.TenantId == _currentUser.TenantId);
        modelBuilder.Entity<WorkflowDefinition>().HasQueryFilter(d => _currentUser == null || d.TenantId == _currentUser.TenantId);
        modelBuilder.Entity<Request>().HasQueryFilter(r => _currentUser == null || r.TenantId == _currentUser.TenantId);
        modelBuilder.Entity<AuditEvent>().HasQueryFilter(e => _currentUser == null || e.TenantId == _currentUser.TenantId);

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GuardAuditEventsAreAppendOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        GuardAuditEventsAreAppendOnly();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Belt-and-suspenders enforcement of the append-only audit trail: even if
    /// application code mistakenly loads and mutates or deletes an AuditEvent, the save
    /// is rejected here rather than silently succeeding.</summary>
    private void GuardAuditEventsAreAppendOnly()
    {
        var offendingEntry = ChangeTracker.Entries<AuditEvent>()
            .FirstOrDefault(e => e.State is EntityState.Modified or EntityState.Deleted);

        if (offendingEntry is not null)
            throw new InvalidOperationException("AuditEvent es append-only: no se permite Update ni Delete.");
    }
}
