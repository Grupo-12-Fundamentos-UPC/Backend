using HairyPaws.Domain.Common.Abstractions;
using HairyPaws.Domain.Identity.Entities;
using HairyPaws.Domain.Notifications.Enums;

namespace HairyPaws.Domain.Notifications.Entities;

public sealed class Notification : Entity
{
    private Notification()
    {
    }

    private Notification(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        string? referenceType,
        Guid? referenceId,
        DateTimeOffset utcNow)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Type = type;
        Title = NormalizeRequired(title);
        Message = NormalizeRequired(message);
        ReferenceType = NormalizeOptional(referenceType);
        ReferenceId = referenceId;
        IsRead = false;
        CreatedAt = utcNow;
    }

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public NotificationType Type { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public string? ReferenceType { get; private set; }

    public Guid? ReferenceId { get; private set; }

    public bool IsRead { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public static Notification Create(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        string? referenceType,
        Guid? referenceId,
        DateTimeOffset utcNow)
    {
        return new Notification(userId, type, title, message, referenceType, referenceId, utcNow);
    }

    public bool IsOwnedBy(Guid userId) => UserId == userId;

    public void MarkAsRead(DateTimeOffset utcNow)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAt = utcNow;
    }

    private static string NormalizeRequired(string value) => value.Trim();

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
