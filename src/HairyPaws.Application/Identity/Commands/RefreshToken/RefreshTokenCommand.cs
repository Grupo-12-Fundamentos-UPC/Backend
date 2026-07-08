using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Contracts.Identity.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Identity.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string Token);

public sealed class RefreshTokenCommandHandler(
    IApplicationDbContext dbContext,
    IJwtTokenService jwtTokenService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = jwtTokenService.ComputeRefreshTokenHash(command.Token);
        var refreshToken = await dbContext.RefreshTokens
            .Include(static token => token.User)
            .ThenInclude(static user => user.RefreshTokens)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        var utcNow = dateTimeProvider.UtcNow;

        if (refreshToken is null || !refreshToken.CanBeUsed(utcNow))
        {
            throw new UnauthorizedAppException("The refresh token is invalid or expired.");
        }

        if (!refreshToken.User.CanLogin())
        {
            throw new UnauthorizedAppException("The user is not allowed to sign in.");
        }

        refreshToken.Revoke(utcNow);

        var accessToken = jwtTokenService.GenerateAccessToken(refreshToken.User);
        var newRefreshToken = jwtTokenService.GenerateRefreshToken();

        var newRefreshTokenEntity = refreshToken.User.AddRefreshToken(newRefreshToken.TokenHash, newRefreshToken.ExpiresAt, utcNow);
        await dbContext.RefreshTokens.AddAsync(newRefreshTokenEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return refreshToken.User.ToAuthResponse(accessToken, newRefreshToken);
    }
}
