using Approva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Approva.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(320);
        builder.Property(u => u.Name).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Role).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(u => u.ApproverRole).HasMaxLength(100);
        builder.Property(u => u.PasswordHash).HasMaxLength(500);

        builder.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Self-referencing manager/delegate — no navigation properties exposed on the
        // domain entity, so these are configured purely via shadow FK relationships.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(u => u.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(u => u.DelegateUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
