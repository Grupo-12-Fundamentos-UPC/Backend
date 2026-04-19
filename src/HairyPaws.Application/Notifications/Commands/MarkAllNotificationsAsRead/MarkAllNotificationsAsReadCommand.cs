using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Security;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Notifications.Commands.MarkAllNotificationsAsRead;

public sealed record MarkAllNotificationsAsReadCommand;

public sealed class MarkAllNotificationsAsReadCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<MarkAllNotificationsAsReadCommand>
{
    public async Task Handle(MarkAllNotificationsAsReadCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var notifications = await dbContext.Notifications
            .Where(entity => entity.UserId == actor.Id && !entity.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.MarkAsRead(dateTimeProvider.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
