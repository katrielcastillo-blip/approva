using Approva.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Approva.Infrastructure.Persistence.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Key).IsRequired().HasMaxLength(200);
        builder.Property(r => r.RequestPath).IsRequired().HasMaxLength(300);
        builder.Property(r => r.ResponseBodyJson).IsRequired().HasColumnType("jsonb");

        builder.HasIndex(r => new { r.TenantId, r.Key, r.RequestPath }).IsUnique();
    }
}
