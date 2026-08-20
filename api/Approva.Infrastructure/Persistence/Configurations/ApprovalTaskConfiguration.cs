using Approva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Approva.Infrastructure.Persistence.Configurations;

public class ApprovalTaskConfiguration : IEntityTypeConfiguration<ApprovalTask>
{
    public void Configure(EntityTypeBuilder<ApprovalTask> builder)
    {
        builder.ToTable("approval_tasks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Status).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(t => t.Comment).HasMaxLength(1000);

        // Optimistic concurrency mapped straight to Postgres' xmin system column: no
        // extra write needed on our side, and any lost-update race between two
        // approvers surfaces as a DbUpdateConcurrencyException -> 409 Conflict.
        builder.Property(t => t.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsRowVersion();

        builder.HasIndex(t => new { t.AssignedToUserId, t.Status });
        builder.HasIndex(t => t.RequestId);

        builder.HasOne<Request>()
            .WithMany()
            .HasForeignKey(t => t.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<WorkflowStep>()
            .WithMany()
            .HasForeignKey(t => t.StepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
