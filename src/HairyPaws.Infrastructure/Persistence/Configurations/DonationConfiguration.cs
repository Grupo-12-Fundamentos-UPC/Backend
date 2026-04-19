using HairyPaws.Domain.Donations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HairyPaws.Infrastructure.Persistence.Configurations;

public sealed class DonationConfiguration : IEntityTypeConfiguration<Donation>
{
    public void Configure(EntityTypeBuilder<Donation> builder)
    {
        builder.ToTable("donations");

        builder.HasKey(donation => donation.Id);
        builder.Property(donation => donation.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(donation => donation.DonationType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(donation => donation.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(donation => donation.Amount)
            .HasPrecision(18, 2);

        builder.Property(donation => donation.TransactionId)
            .HasMaxLength(100);

        builder.Property(donation => donation.Notes)
            .HasMaxLength(2000);

        builder.Property(donation => donation.ReceiptPath)
            .HasMaxLength(500);

        builder.Property(donation => donation.ConfirmedAt)
            .HasColumnType("timestamptz");

        builder.Property(donation => donation.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(donation => donation.UpdatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(donation => donation.OrganizationId);
        builder.HasIndex(donation => donation.DonorUserId);
        builder.HasIndex(donation => donation.Status);
        builder.HasIndex(donation => donation.CreatedAt);

        builder.HasOne(donation => donation.DonorUser)
            .WithMany()
            .HasForeignKey(donation => donation.DonorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(donation => donation.Organization)
            .WithMany()
            .HasForeignKey(donation => donation.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(donation => donation.ConfirmedByUser)
            .WithMany()
            .HasForeignKey(donation => donation.ConfirmedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(donation => donation.Items)
            .WithOne(item => item.Donation)
            .HasForeignKey(item => item.DonationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Donation.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
