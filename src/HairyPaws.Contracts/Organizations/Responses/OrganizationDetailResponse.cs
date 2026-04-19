namespace HairyPaws.Contracts.Organizations.Responses;

public sealed record OrganizationDetailResponse(
    Guid Id,
    string Name,
    string Ruc,
    string? Description,
    string? Address,
    string? Phone,
    string? Email,
    string? LogoPath,
    string VerificationStatus,
    IReadOnlyCollection<OrganizationDocumentResponse> Documents,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
