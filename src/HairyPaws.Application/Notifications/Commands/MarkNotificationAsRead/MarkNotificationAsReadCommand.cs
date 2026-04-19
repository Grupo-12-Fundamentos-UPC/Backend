using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Notifications.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Notifications.Commands.MarkNotificationAsRead;

public sealed record MarkNotificationAsReadCommand(Guid NotificationId);

public sealed class MarkNotificationAsReadCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<MarkNotificationAsReadCommand, NotificationResponse>
{
    public async Task<NotificationResponse> Handle(MarkNotificationAsReadCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var notification = await dbContext.Notifications
            .SingleOrDefaultAsync(entity => entity.Id == command.NotificationId, cancellationToken)
            ?? throw new NotFoundException("The notification was not found.");

        if (!CurrentUserContext.CanAccessNotification(actor, notification))
        {
            throw new ForbiddenAppException("You are not allowed to access this notification.");
        }

        notification.MarkAsRead(dateTimeProvider.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return notification.ToResponse();
    }
}
