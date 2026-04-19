using FluentAssertions;
using HairyPaws.Domain.Events.Entities;
using HairyPaws.Domain.Events.Enums;

namespace HairyPaws.Tests.Unit.Domain;

public sealed class EventTests
{
    [Fact]
    public void Create_ShouldStartAsDraft()
    {
        var eventEntity = Event.Create(
            Guid.NewGuid(),
            "Adoption Fair",
            "Weekend adoption event.",
            DateTimeOffset.UtcNow.AddDays(5),
            "City Park",
            isVolunteerEvent: true,
            DateTimeOffset.UtcNow);

        eventEntity.Status.Should().Be(EventStatus.Draft);
        eventEntity.CanPublish().Should().BeTrue();
        eventEntity.CanCancel().Should().BeTrue();
        eventEntity.IsPubliclyVisible().Should().BeFalse();
    }

    [Fact]
    public void GetPublishValidationErrors_ShouldRequireFutureDateTitleAndDescription()
    {
        var eventEntity = Event.Create(
            Guid.NewGuid(),
            " ",
            " ",
            DateTimeOffset.UtcNow.AddMinutes(-1),
            null,
            isVolunteerEvent: false,
            DateTimeOffset.UtcNow.AddDays(-2));

        var errors = eventEntity.GetPublishValidationErrors(DateTimeOffset.UtcNow);

        errors.Should().Contain(error => error.Contains("Title"));
        errors.Should().Contain(error => error.Contains("Description"));
        errors.Should().Contain(error => error.Contains("future", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PublishAndCancel_ShouldUpdateStateAsExpected()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var eventEntity = Event.Create(
            Guid.NewGuid(),
            "Volunteer Day",
            "Support the shelter.",
            utcNow.AddDays(10),
            "Shelter",
            isVolunteerEvent: true,
            utcNow);

        eventEntity.Publish(utcNow.AddHours(1));

        eventEntity.Status.Should().Be(EventStatus.Published);
        eventEntity.IsPubliclyVisible().Should().BeTrue();

        eventEntity.Cancel(utcNow.AddHours(2));

        eventEntity.Status.Should().Be(EventStatus.Cancelled);
        eventEntity.CanPublish().Should().BeFalse();
        eventEntity.IsPubliclyVisible().Should().BeFalse();
    }
}
