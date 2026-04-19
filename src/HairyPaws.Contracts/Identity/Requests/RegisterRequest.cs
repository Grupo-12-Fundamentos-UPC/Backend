namespace HairyPaws.Contracts.Identity.Requests;

public sealed record RegisterRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public string? IdentityDocument { get; init; }

    public string? Address { get; init; }
}
