using HairyPaws.Contracts.Notifications.Responses;
using HairyPaws.Domain.Notifications.Entities;

namespace HairyPaws.Application.Common.Mappings;

public static class NotificationResponseMappings
{
    public static NotificationResponse ToResponse(this Notification notification)
    {
        return new NotificationResponse(
            notification.Id,
            notification.Type.ToString(),
            notification.Title,
            notification.Message,
            notification.ReferenceType,
            notification.ReferenceId,
            notification.IsRead,
            notification.CreatedAt,
            notification.ReadAt);
    }
}
