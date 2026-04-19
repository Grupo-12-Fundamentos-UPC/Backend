using HairyPaws.Domain.Pets.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HairyPaws.Infrastructure.Persistence.Configurations;

public sealed class PetConfiguration : IEntityTypeConfiguration<Pet>
{
    public void Configure(EntityTypeBuilder<Pet> builder)
    {
        builder.ToTable(
            "pets",
            tableBuilder => tableBuilder.HasCheckConstraint(
                "ck_pets_owner_or_organization",
                "(\"owner_user_id\" IS NOT NULL AND \"organization_id\" IS NULL) OR (\"owner_user_id\" IS NULL AND \"organization_id\" IS NOT NULL)"));

        builder.HasKey(pet => pet.Id);
        builder.Property(pet => pet.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(pet => pet.Name)
            .HasMaxLength(150);

        builder.Property(pet => pet.Species)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(pet => pet.Breed)
            .HasMaxLength(150);

        builder.Property(pet => pet.AgeText)
            .HasMaxLength(100);

        builder.Property(pet => pet.Sex)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(pet => pet.Size)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(pet => pet.Description)
            .HasMaxLength(4000);

        builder.Property(pet => pet.Temperament)
            .HasMaxLength(1000);

        builder.Property(pet => pet.MedicalHistory)
            .HasMaxLength(2000);

        builder.Property(pet => pet.LocationDistrict)
            .HasMaxLength(150);

        builder.Property(pet => pet.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(pet => pet.PublishedAt)
            .HasColumnType("timestamptz");

        builder.Property(pet => pet.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(pet => pet.UpdatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(pet => pet.DeletedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(pet => pet.Status);
        builder.HasIndex(pet => new { pet.Species, pet.Status });
        builder.HasIndex(pet => pet.LocationDistrict);

        builder.HasOne(pet => pet.OwnerUser)
            .WithMany()
            .HasForeignKey(pet => pet.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pet => pet.Organization)
            .WithMany()
            .HasForeignKey(pet => pet.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(pet => pet.Photos)
            .WithOne(photo => photo.Pet)
            .HasForeignKey(photo => photo.PetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Pet.Photos))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
