using FluentAssertions;
using HairyPaws.Domain.Visits.Entities;
using HairyPaws.Domain.Visits.Enums;

namespace HairyPaws.Tests.Unit.Domain;

public sealed class VisitTests
{
    [Fact]
    public void Create_ShouldStartPendingAndActive()
    {
        var visit = Visit.Create(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(2),
            "Shelter office",
            "Please confirm attendance",
            DateTimeOffset.UtcNow);

        visit.Status.Should().Be(VisitStatus.Pending);
        visit.HasActiveStatus().Should().BeTrue();
        visit.CanBeApprovedByAdopter().Should().BeTrue();
        visit.CanBeRejectedByAdopter().Should().BeTrue();
        visit.CanBeCancelledByManager().Should().BeTrue();
        visit.CanBeCompletedByManager().Should().BeFalse();
    }

    [Fact]
    public void Approve_ShouldAllowCompletion()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var visit = Visit.Create(
            Guid.NewGuid(),
            utcNow.AddDays(2),
            "Shelter office",
            null,
            utcNow);

        visit.Approve("Confirmed", utcNow.AddMinutes(5));

        visit.Status.Should().Be(VisitStatus.Approved);
        visit.HasActiveStatus().Should().BeTrue();
        visit.CanBeCompletedByManager().Should().BeTrue();
    }

    [Fact]
    public void Reject_ShouldEndVisit()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var visit = Visit.Create(
            Guid.NewGuid(),
            utcNow.AddDays(2),
            null,
            null,
            utcNow);

        visit.Reject("Cannot attend", utcNow.AddMinutes(5));

        visit.Status.Should().Be(VisitStatus.Rejected);
        visit.HasActiveStatus().Should().BeFalse();
        visit.CanBeCompletedByManager().Should().BeFalse();
        visit.CanBeCancelledByManager().Should().BeFalse();
    }

    [Fact]
    public void Cancel_ShouldEndVisit()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var visit = Visit.Create(
            Guid.NewGuid(),
            utcNow.AddDays(2),
            null,
            null,
            utcNow);

        visit.Cancel("Manager cancelled", utcNow.AddMinutes(5));

        visit.Status.Should().Be(VisitStatus.Cancelled);
        visit.HasActiveStatus().Should().BeFalse();
        visit.CanBeCompletedByManager().Should().BeFalse();
    }

    [Fact]
    public void Complete_ShouldEndVisit()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var visit = Visit.Create(
            Guid.NewGuid(),
            utcNow.AddDays(2),
            null,
            null,
            utcNow);

        visit.Approve("Confirmed", utcNow.AddMinutes(5));
        visit.Complete("Completed successfully", utcNow.AddMinutes(15));

        visit.Status.Should().Be(VisitStatus.Completed);
        visit.HasActiveStatus().Should().BeFalse();
        visit.CanBeCompletedByManager().Should().BeFalse();
        visit.CanBeCancelledByManager().Should().BeFalse();
    }
}
