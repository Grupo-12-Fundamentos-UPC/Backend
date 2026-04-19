using HairyPaws.Domain.Organizations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HairyPaws.Infrastructure.Persistence.Configurations;

public sealed class OrganizationDocumentConfiguration : IEntityTypeConfiguration<OrganizationDocument>
{
    public void Configure(EntityTypeBuilder<OrganizationDocument> builder)
    {
        builder.ToTable("organization_documents");

        builder.HasKey(document => document.Id);
        builder.Property(document => document.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(document => document.DocumentType)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(document => document.FilePath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(document => document.UploadedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(document => document.OrganizationId);
    }
}
