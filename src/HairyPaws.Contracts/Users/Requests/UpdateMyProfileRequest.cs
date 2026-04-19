namespace HairyPaws.Contracts.Users.Requests;

public sealed record UpdateMyProfileRequest
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public string? IdentityDocument { get; init; }

    public string? Address { get; init; }

    public string? ProfileImagePath { get; init; }
}
