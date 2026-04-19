using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Contracts.Identity.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Identity.Commands.Login;

public sealed record LoginCommand(string Email, string Password);

public sealed class LoginCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users
            .Include(static user => user.RefreshTokens)
            .SingleOrDefaultAsync(user => user.Email == normalizedEmail && user.DeletedAt == null, cancellationToken);

        if (user is null || !passwordHasher.VerifyHashedPassword(user, user.PasswordHash, command.Password))
        {
            throw new UnauthorizedAppException("Invalid credentials.");
        }

        if (!user.CanLogin())
        {
            throw new UnauthorizedAppException("The user is not allowed to sign in.");
        }

        var utcNow = dateTimeProvider.UtcNow;
        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var refreshToken = jwtTokenService.GenerateRefreshToken();

        var refreshTokenEntity = user.AddRefreshToken(refreshToken.TokenHash, refreshToken.ExpiresAt, utcNow);
        await dbContext.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return user.ToAuthResponse(accessToken, refreshToken);
    }
}
