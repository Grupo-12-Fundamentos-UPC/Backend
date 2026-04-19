using HairyPaws.Domain.Audit.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HairyPaws.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(entity => entity.EntityName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(entity => entity.Action)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(entity => entity.BeforeJson)
            .HasColumnType("text");

        builder.Property(entity => entity.AfterJson)
            .HasColumnType("text");

        builder.Property(entity => entity.MetadataJson)
            .HasColumnType("text");

        builder.Property(entity => entity.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(entity => new { entity.EntityName, entity.EntityId });
        builder.HasIndex(entity => entity.PerformedByUserId);
        builder.HasIndex(entity => entity.CreatedAt);

        builder.HasOne(entity => entity.PerformedByUser)
            .WithMany()
            .HasForeignKey(entity => entity.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
