namespace HairyPaws.Contracts.Users.Requests;

public sealed record UpdateUserVerificationRequest
{
    public string VerificationStatus { get; init; } = string.Empty;
}
