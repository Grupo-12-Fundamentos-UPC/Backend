using HairyPaws.Domain.Common.Abstractions;
using HairyPaws.Domain.Identity.Entities;
using HairyPaws.Domain.Identity.Enums;
using HairyPaws.Domain.Organizations.Enums;

namespace HairyPaws.Domain.Organizations.Entities;

public sealed class Organization : AuditableEntity
{
    private readonly List<OrganizationDocument> _documents = [];

    private Organization()
    {
    }

    private Organization(
        Guid ownerUserId,
        string name,
        string ruc,
        DateTimeOffset utcNow,
        string? description,
        string? address,
        string? phone,
        string? email)
    {
        Id = Guid.NewGuid();
        OwnerUserId = ownerUserId;
        Name = NormalizeRequired(name);
        Ruc = NormalizeRequired(ruc);
        Description = NormalizeOptional(description);
        Address = NormalizeOptional(address);
        Phone = NormalizeOptional(phone);
        Email = NormalizeOptional(email)?.ToLowerInvariant();
        VerificationStatus = VerificationStatus.Pending;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public Guid OwnerUserId { get; private set; }

    public User OwnerUser { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public string Ruc { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string? Address { get; private set; }

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public string? LogoPath { get; private set; }

    public VerificationStatus VerificationStatus { get; private set; }

    public IReadOnlyCollection<OrganizationDocument> Documents => _documents;

    public static Organization Create(
        Guid ownerUserId,
        string name,
        string ruc,
        DateTimeOffset utcNow,
        string? description = null,
        string? address = null,
        string? phone = null,
        string? email = null)
    {
        return new Organization(ownerUserId, name, ruc, utcNow, description, address, phone, email);
    }

    public bool IsOwnedBy(Guid userId) => OwnerUserId == userId;

    public bool IsVisibleToPublic() => DeletedAt is null && VerificationStatus == VerificationStatus.Verified;

    public void Update(
        string name,
        string ruc,
        string? description,
        string? address,
        string? phone,
        string? email,
        DateTimeOffset utcNow)
    {
        Name = NormalizeRequired(name);
        Ruc = NormalizeRequired(ruc);
        Description = NormalizeOptional(description);
        Address = NormalizeOptional(address);
        Phone = NormalizeOptional(phone);
        Email = NormalizeOptional(email)?.ToLowerInvariant();
        UpdatedAt = utcNow;
    }

    public void SetLogo(string? logoPath, DateTimeOffset utcNow)
    {
        LogoPath = NormalizeOptional(logoPath);
        UpdatedAt = utcNow;
    }

    public OrganizationDocument AddDocument(
        OrganizationDocumentType documentType,
        string filePath,
        DateTimeOffset utcNow)
    {
        var document = OrganizationDocument.Create(Id, documentType, filePath, utcNow);
        _documents.Add(document);
        UpdatedAt = utcNow;
        return document;
    }

    public void RemoveDocument(OrganizationDocument document, DateTimeOffset utcNow)
    {
        _documents.Remove(document);
        UpdatedAt = utcNow;
    }

    public void UpdateVerificationStatus(VerificationStatus verificationStatus, DateTimeOffset utcNow)
    {
        VerificationStatus = verificationStatus;
        UpdatedAt = utcNow;
    }

    private static string NormalizeRequired(string value) => value.Trim();

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
