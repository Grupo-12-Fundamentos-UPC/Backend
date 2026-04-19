using HairyPaws.Domain.Identity.Entities;

namespace HairyPaws.Application.Common.Interfaces;

public interface IJwtTokenService
{
    AccessTokenResult GenerateAccessToken(User user);

    RefreshTokenResult GenerateRefreshToken();

    string ComputeRefreshTokenHash(string refreshToken);
}
