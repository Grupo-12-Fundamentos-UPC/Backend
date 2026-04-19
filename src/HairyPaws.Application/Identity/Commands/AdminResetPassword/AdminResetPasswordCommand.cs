using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Security;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Identity.Commands.AdminResetPassword;

public sealed record AdminResetPasswordCommand(Guid UserId, string NewPassword);

public sealed class AdminResetPasswordCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService,
    ICurrentUserService currentUserService)
    : ICommandHandler<AdminResetPasswordCommand>
{
    public async Task Handle(AdminResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserContext.GetRequiredUserId(currentUserService);
        var user = await dbContext.Users
            .Include(static entity => entity.RefreshTokens)
            .SingleOrDefaultAsync(entity => entity.Id == command.UserId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The requested user was not found.");

        var before = user.ToAuditSnapshot();
        var utcNow = dateTimeProvider.UtcNow;
        user.ChangePassword(passwordHasher.HashPassword(user, command.NewPassword), utcNow);
        user.RevokeAllActiveRefreshTokens(utcNow);

        await auditService.WriteAsync(
            "ResetPassword",
            actorUserId,
            "User",
            user.Id,
            before,
            user.ToAuditSnapshot(),
            new
            {
                targetEmail = user.Email,
                revokedRefreshTokens = user.RefreshTokens.Count
            },
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
