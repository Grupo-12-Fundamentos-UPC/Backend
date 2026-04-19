using HairyPaws.Domain.Organizations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HairyPaws.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");

        builder.HasKey(organization => organization.Id);
        builder.Property(organization => organization.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(organization => organization.OwnerUserId)
            .IsRequired();

        builder.Property(organization => organization.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(organization => organization.Ruc)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(organization => organization.Description)
            .HasMaxLength(2000);

        builder.Property(organization => organization.Address)
            .HasMaxLength(500);

        builder.Property(organization => organization.Phone)
            .HasMaxLength(30);

        builder.Property(organization => organization.Email)
            .HasMaxLength(320);

        builder.Property(organization => organization.LogoPath)
            .HasMaxLength(500);

        builder.Property(organization => organization.VerificationStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(organization => organization.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(organization => organization.UpdatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(organization => organization.DeletedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(organization => organization.Ruc)
            .IsUnique();

        builder.HasIndex(organization => organization.OwnerUserId)
            .IsUnique();

        builder.HasIndex(organization => organization.VerificationStatus);

        builder.HasOne(organization => organization.OwnerUser)
            .WithOne()
            .HasForeignKey<Organization>(organization => organization.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(organization => organization.Documents)
            .WithOne(document => document.Organization)
            .HasForeignKey(document => document.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Organization.Documents))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
