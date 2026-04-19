namespace HairyPaws.Contracts.Identity.Requests;

public sealed record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
