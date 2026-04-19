namespace HairyPaws.Contracts.Users.Responses;

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string Status,
    string VerificationStatus,
    string? PhoneNumber,
    string? IdentityDocument,
    string? Address,
    string? ProfileImagePath,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
