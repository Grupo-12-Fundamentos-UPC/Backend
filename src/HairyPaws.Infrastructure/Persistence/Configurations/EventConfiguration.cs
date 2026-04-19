using HairyPaws.Domain.Events.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HairyPaws.Infrastructure.Persistence.Configurations;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(entity => entity.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(entity => entity.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(entity => entity.EventDate)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(entity => entity.Location)
            .HasMaxLength(500);

        builder.Property(entity => entity.ImagePath)
            .HasMaxLength(500);

        builder.Property(entity => entity.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(entity => entity.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(entity => entity.UpdatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(entity => entity.DeletedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(entity => entity.OrganizationId);
        builder.HasIndex(entity => entity.Status);
        builder.HasIndex(entity => entity.EventDate);

        builder.HasOne(entity => entity.Organization)
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
