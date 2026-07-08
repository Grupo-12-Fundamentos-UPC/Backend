using HairyPaws.Domain.Notifications.Enums;

namespace HairyPaws.Application.Common.Ports;

public interface INotificationService
{
    Task CreateAsync(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        string? referenceType,
        Guid? referenceId,
        CancellationToken cancellationToken);
}
