using HairyPaws.Domain.Common.Abstractions;
using HairyPaws.Domain.Organizations.Enums;

namespace HairyPaws.Domain.Organizations.Entities;

public sealed class OrganizationDocument : Entity
{
    private OrganizationDocument()
    {
    }

    private OrganizationDocument(
        Guid organizationId,
        OrganizationDocumentType documentType,
        string filePath,
        DateTimeOffset uploadedAt)
    {
        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        DocumentType = documentType;
        FilePath = NormalizeRequired(filePath);
        UploadedAt = uploadedAt;
    }

    public Guid OrganizationId { get; private set; }

    public Organization Organization { get; private set; } = null!;

    public OrganizationDocumentType DocumentType { get; private set; }

    public string FilePath { get; private set; } = string.Empty;

    public DateTimeOffset UploadedAt { get; private set; }

    public static OrganizationDocument Create(
        Guid organizationId,
        OrganizationDocumentType documentType,
        string filePath,
        DateTimeOffset uploadedAt)
    {
        return new OrganizationDocument(organizationId, documentType, filePath, uploadedAt);
    }

    private static string NormalizeRequired(string value) => value.Trim();
}
