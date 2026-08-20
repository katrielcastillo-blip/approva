using Approva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Approva.Infrastructure.Persistence.Configurations;

public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("workflow_definitions");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.EntityType).IsRequired().HasMaxLength(100);

        builder.HasIndex(d => new { d.TenantId, d.EntityType, d.IsActive });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(d => d.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Steps is exposed as IReadOnlyCollection<WorkflowStep> to keep the aggregate
        // encapsulated; EF Core mutates the private `_steps` backing field directly.
        builder.HasMany(d => d.Steps)
            .WithOne()
            .HasForeignKey(s => s.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(WorkflowDefinition.Steps))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
