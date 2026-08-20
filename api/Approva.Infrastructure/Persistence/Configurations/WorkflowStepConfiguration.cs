using Approva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Approva.Infrastructure.Persistence.Configurations;

public class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
    public void Configure(EntityTypeBuilder<WorkflowStep> builder)
    {
        builder.ToTable("workflow_steps");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.ApproverType).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.ApproverRef).HasMaxLength(200);
        builder.Property(s => s.EscalationPolicy).IsRequired().HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(s => new { s.WorkflowDefinitionId, s.Order }).IsUnique();

        builder.HasMany(s => s.Conditions)
            .WithOne()
            .HasForeignKey(c => c.WorkflowStepId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(WorkflowStep.Conditions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
