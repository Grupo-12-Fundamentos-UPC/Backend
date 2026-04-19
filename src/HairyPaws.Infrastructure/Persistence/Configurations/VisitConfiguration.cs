using HairyPaws.Domain.Visits.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HairyPaws.Infrastructure.Persistence.Configurations;

public sealed class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.ToTable("visits");

        builder.HasKey(visit => visit.Id);
        builder.Property(visit => visit.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(visit => visit.ScheduledAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(visit => visit.Location)
            .HasMaxLength(500);

        builder.Property(visit => visit.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(visit => visit.Notes)
            .HasMaxLength(1000);

        builder.Property(visit => visit.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(visit => visit.UpdatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex([nameof(Visit.AdoptionRequestId)], "ix_visits_adoption_request_id")
            .HasDatabaseName("ix_visits_adoption_request_id");
        builder.HasIndex(visit => visit.Status);
        builder.HasIndex(visit => visit.ScheduledAt);

        builder.HasIndex([nameof(Visit.AdoptionRequestId)], "ux_visits_adoption_request_id_single_active")
            .IsUnique()
            .HasDatabaseName("ux_visits_adoption_request_id_single_active")
            .HasFilter("\"status\" IN ('Pending', 'Approved')");
    }
}
