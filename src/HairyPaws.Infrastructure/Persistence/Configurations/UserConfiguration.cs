using HairyPaws.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HairyPaws.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(user => user.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(user => user.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(user => user.VerificationStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(user => user.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(user => user.IdentityDocument)
            .HasMaxLength(50);

        builder.Property(user => user.Address)
            .HasMaxLength(500);

        builder.Property(user => user.ProfileImagePath)
            .HasMaxLength(500);

        builder.Property(user => user.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(user => user.UpdatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(user => user.DeletedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.HasIndex(user => user.IdentityDocument)
            .IsUnique()
            .HasFilter("\"identity_document\" IS NOT NULL");

        builder.HasIndex(user => user.Role);
        builder.HasIndex(user => user.Status);
        builder.HasIndex(user => user.VerificationStatus);

        builder.HasMany(user => user.RefreshTokens)
            .WithOne(token => token.User)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(User.RefreshTokens))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
