using HairyPaws.Application.Common.Ports;
using HairyPaws.Domain.Notifications.Entities;
using HairyPaws.Domain.Notifications.Enums;

namespace HairyPaws.Application.Notifications.Services;

public sealed class NotificationService(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : INotificationService
{
    public async Task CreateAsync(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        string? referenceType,
        Guid? referenceId,
        CancellationToken cancellationToken)
    {
        var notification = Notification.Create(
            userId,
            type,
            title,
            message,
            referenceType,
            referenceId,
            dateTimeProvider.UtcNow);

        await dbContext.Notifications.AddAsync(notification, cancellationToken);
    }
}
