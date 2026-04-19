using HairyPaws.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HairyPaws.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(token => token.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(token => token.ExpiresAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(token => token.RevokedAt)
            .HasColumnType("timestamptz");

        builder.Property(token => token.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(token => token.UserId);
        builder.HasIndex(token => token.ExpiresAt);
        builder.HasIndex(token => token.TokenHash)
            .IsUnique();
    }
}
