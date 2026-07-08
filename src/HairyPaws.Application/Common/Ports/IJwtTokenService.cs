using HairyPaws.Domain.Identity.Entities;

namespace HairyPaws.Application.Common.Ports;

public interface IJwtTokenService
{
    AccessTokenResult GenerateAccessToken(User user);

    RefreshTokenResult GenerateRefreshToken();

    string ComputeRefreshTokenHash(string refreshToken);
}
