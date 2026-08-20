using Approva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Approva.Infrastructure.Persistence.Configurations;

public class WorkflowConditionConfiguration : IEntityTypeConfiguration<WorkflowCondition>
{
    public void Configure(EntityTypeBuilder<WorkflowCondition> builder)
    {
        builder.ToTable("workflow_conditions");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Field).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Operator).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.Value).IsRequired().HasMaxLength(500);
    }
}
