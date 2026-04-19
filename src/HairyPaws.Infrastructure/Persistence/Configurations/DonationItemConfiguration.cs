using HairyPaws.Domain.Donations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HairyPaws.Infrastructure.Persistence.Configurations;

public sealed class DonationItemConfiguration : IEntityTypeConfiguration<DonationItem>
{
    public void Configure(EntityTypeBuilder<DonationItem> builder)
    {
        builder.ToTable("donation_items");

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(item => item.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(item => item.Description)
            .HasMaxLength(1000);

        builder.Property(item => item.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(item => item.DonationId);
    }
}
