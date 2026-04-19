namespace HairyPaws.Contracts.Notifications.Responses;

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    string Title,
    string Message,
    string? ReferenceType,
    Guid? ReferenceId,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);
