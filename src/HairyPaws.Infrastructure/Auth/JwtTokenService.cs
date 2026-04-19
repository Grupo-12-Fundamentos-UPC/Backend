using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Domain.Identity.Entities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HairyPaws.Infrastructure.Auth;

public sealed class JwtTokenService(
    IDateTimeProvider dateTimeProvider,
    IOptions<JwtOptions> jwtOptions)
    : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public AccessTokenResult GenerateAccessToken(User user)
    {
        var utcNow = dateTimeProvider.UtcNow;
        var expiresAt = utcNow.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("status", user.Status.ToString()),
            new Claim("verification_status", user.VerificationStatus.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: utcNow.AddSeconds(-5).UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public RefreshTokenResult GenerateRefreshToken()
    {
        var utcNow = dateTimeProvider.UtcNow;
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        return new RefreshTokenResult(
            token,
            ComputeRefreshTokenHash(token),
            utcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays));
    }

    public string ComputeRefreshTokenHash(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }
}
