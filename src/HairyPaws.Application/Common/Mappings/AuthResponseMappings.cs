using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Contracts.Identity.Responses;
using HairyPaws.Domain.Identity.Entities;

namespace HairyPaws.Application.Common.Mappings;

public static class AuthResponseMappings
{
    public static AuthResponse ToAuthResponse(this User user, AccessTokenResult accessToken, RefreshTokenResult refreshToken)
    {
        return new AuthResponse(
            accessToken.Token,
            refreshToken.Token,
            accessToken.ExpiresAt,
            refreshToken.ExpiresAt,
            user.ToSummaryResponse());
    }
}
