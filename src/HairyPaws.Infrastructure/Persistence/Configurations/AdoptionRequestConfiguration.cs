using HairyPaws.Domain.Adoption.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HairyPaws.Infrastructure.Persistence.Configurations;

public sealed class AdoptionRequestConfiguration : IEntityTypeConfiguration<AdoptionRequest>
{
    public void Configure(EntityTypeBuilder<AdoptionRequest> builder)
    {
        builder.ToTable("adoption_requests");

        builder.HasKey(adoptionRequest => adoptionRequest.Id);
        builder.Property(adoptionRequest => adoptionRequest.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(adoptionRequest => adoptionRequest.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(adoptionRequest => adoptionRequest.ContactPhone)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(adoptionRequest => adoptionRequest.LivingConditions)
            .HasMaxLength(2000);

        builder.Property(adoptionRequest => adoptionRequest.WhyAdopt)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(adoptionRequest => adoptionRequest.ReviewNotes)
            .HasMaxLength(2000);

        builder.Property(adoptionRequest => adoptionRequest.ReviewedAt)
            .HasColumnType("timestamptz");

        builder.Property(adoptionRequest => adoptionRequest.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(adoptionRequest => adoptionRequest.UpdatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex([nameof(AdoptionRequest.PetId)], "ix_adoption_requests_pet_id")
            .HasDatabaseName("ix_adoption_requests_pet_id");
        builder.HasIndex(adoptionRequest => adoptionRequest.AdopterUserId);
        builder.HasIndex(adoptionRequest => adoptionRequest.Status);
        builder.HasIndex(adoptionRequest => adoptionRequest.CreatedAt);

        builder.HasIndex(adoptionRequest => new { adoptionRequest.PetId, adoptionRequest.AdopterUserId })
            .IsUnique()
            .HasDatabaseName("ux_adoption_requests_pet_id_adopter_user_id_active")
            .HasFilter("\"status\" IN ('Submitted', 'UnderReview', 'Approved')");

        builder.HasIndex([nameof(AdoptionRequest.PetId)], "ux_adoption_requests_pet_id_single_approved")
            .IsUnique()
            .HasDatabaseName("ux_adoption_requests_pet_id_single_approved")
            .HasFilter("\"status\" = 'Approved'");

        builder.HasOne(adoptionRequest => adoptionRequest.Pet)
            .WithMany()
            .HasForeignKey(adoptionRequest => adoptionRequest.PetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(adoptionRequest => adoptionRequest.AdopterUser)
            .WithMany()
            .HasForeignKey(adoptionRequest => adoptionRequest.AdopterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(adoptionRequest => adoptionRequest.ReviewedByUser)
            .WithMany()
            .HasForeignKey(adoptionRequest => adoptionRequest.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(adoptionRequest => adoptionRequest.Visits)
            .WithOne(visit => visit.AdoptionRequest)
            .HasForeignKey(visit => visit.AdoptionRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(AdoptionRequest.Visits))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
