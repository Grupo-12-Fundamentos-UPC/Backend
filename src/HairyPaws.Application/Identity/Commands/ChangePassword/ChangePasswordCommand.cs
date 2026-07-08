using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Identity.Commands.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword);

public sealed class ChangePasswordCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAppException("Authentication is required.");

        var user = await dbContext.Users
            .Include(static entity => entity.RefreshTokens)
            .SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken)
            ?? throw new NotFoundException("The current user was not found.");

        if (!passwordHasher.VerifyHashedPassword(user, user.PasswordHash, command.CurrentPassword))
        {
            throw new UnauthorizedAppException("The current password is invalid.");
        }

        if (command.CurrentPassword == command.NewPassword)
        {
            throw new BusinessRuleViolationException("The new password must be different from the current password.");
        }

        var utcNow = dateTimeProvider.UtcNow;
        user.ChangePassword(passwordHasher.HashPassword(user, command.NewPassword), utcNow);
        user.RevokeAllActiveRefreshTokens(utcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
