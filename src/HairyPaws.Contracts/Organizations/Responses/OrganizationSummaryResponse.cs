namespace HairyPaws.Contracts.Organizations.Responses;

public sealed record OrganizationSummaryResponse(
    Guid Id,
    string Name,
    string Ruc,
    string? Description,
    string? Address,
    string? Phone,
    string? Email,
    string? LogoPath,
    string VerificationStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
