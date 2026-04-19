using FluentAssertions;
using HairyPaws.Domain.Notifications.Entities;
using HairyPaws.Domain.Notifications.Enums;

namespace HairyPaws.Tests.Unit.Domain;

public sealed class NotificationTests
{
    [Fact]
    public void Create_ShouldStartUnread()
    {
        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.Generic,
            "Title",
            "Message",
            "Donation",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
    }

    [Fact]
    public void MarkAsRead_ShouldSetReadFlags()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.Generic,
            "Title",
            "Message",
            null,
            null,
            utcNow);

        notification.MarkAsRead(utcNow.AddMinutes(1));

        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().Be(utcNow.AddMinutes(1));
    }

    [Fact]
    public void MarkAsRead_ShouldBeIdempotent()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.Generic,
            "Title",
            "Message",
            null,
            null,
            utcNow);

        notification.MarkAsRead(utcNow.AddMinutes(1));
        notification.MarkAsRead(utcNow.AddMinutes(5));

        notification.ReadAt.Should().Be(utcNow.AddMinutes(1));
    }
}
