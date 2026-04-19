using HairyPaws.Domain.Pets.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HairyPaws.Infrastructure.Persistence.Configurations;

public sealed class PetPhotoConfiguration : IEntityTypeConfiguration<PetPhoto>
{
    public void Configure(EntityTypeBuilder<PetPhoto> builder)
    {
        builder.ToTable("pet_photos");

        builder.HasKey(photo => photo.Id);
        builder.Property(photo => photo.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(photo => photo.FilePath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(photo => photo.SortOrder)
            .IsRequired();

        builder.Property(photo => photo.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(photo => photo.PetId);
    }
}
