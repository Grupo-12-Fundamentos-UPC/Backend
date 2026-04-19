using HairyPaws.Contracts.Users.Responses;

namespace HairyPaws.Contracts.Identity.Responses;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt,
    UserSummaryResponse User);
