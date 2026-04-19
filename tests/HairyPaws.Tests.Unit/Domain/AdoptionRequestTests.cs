using FluentAssertions;
using HairyPaws.Domain.Adoption.Entities;
using HairyPaws.Domain.Adoption.Enums;

namespace HairyPaws.Tests.Unit.Domain;

public sealed class AdoptionRequestTests
{
    [Fact]
    public void Create_ShouldStartInSubmittedState()
    {
        var utcNow = DateTimeOffset.UtcNow;

        var adoptionRequest = AdoptionRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "5554444",
            "Apartment",
            hasPreviousPets: true,
            "Ready to adopt",
            utcNow);

        adoptionRequest.Status.Should().Be(AdoptionRequestStatus.Submitted);
        adoptionRequest.HasActiveStatus().Should().BeTrue();
        adoptionRequest.CanStartReview().Should().BeTrue();
        adoptionRequest.CanApprove().Should().BeTrue();
        adoptionRequest.CanReject().Should().BeTrue();
        adoptionRequest.CanCancelByAdopter().Should().BeTrue();
        adoptionRequest.CanCreateVisit().Should().BeFalse();
        adoptionRequest.CanComplete().Should().BeFalse();
    }

    [Fact]
    public void StartReview_ShouldEnableVisitCreation()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var reviewerId = Guid.NewGuid();
        var adoptionRequest = AdoptionRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "5554444",
            null,
            hasPreviousPets: false,
            "Ready to adopt",
            utcNow);

        adoptionRequest.StartReview(reviewerId, "Checking profile", utcNow.AddMinutes(10));

        adoptionRequest.Status.Should().Be(AdoptionRequestStatus.UnderReview);
        adoptionRequest.ReviewedByUserId.Should().Be(reviewerId);
        adoptionRequest.CanStartReview().Should().BeFalse();
        adoptionRequest.CanApprove().Should().BeTrue();
        adoptionRequest.CanReject().Should().BeTrue();
        adoptionRequest.CanCancelByAdopter().Should().BeTrue();
        adoptionRequest.CanCreateVisit().Should().BeTrue();
    }

    [Fact]
    public void Approve_ShouldMakeRequestCompletable_AndNotCancellableByAdopter()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var reviewerId = Guid.NewGuid();
        var adoptionRequest = AdoptionRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "5554444",
            null,
            hasPreviousPets: true,
            "Ready to adopt",
            utcNow);

        adoptionRequest.Approve(reviewerId, "Approved", utcNow.AddMinutes(5));

        adoptionRequest.Status.Should().Be(AdoptionRequestStatus.Approved);
        adoptionRequest.CanComplete().Should().BeTrue();
        adoptionRequest.CanCancelByAdopter().Should().BeFalse();
        adoptionRequest.HasActiveStatus().Should().BeTrue();
    }

    [Fact]
    public void Complete_ShouldCloseRequest()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var reviewerId = Guid.NewGuid();
        var adoptionRequest = AdoptionRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "5554444",
            null,
            hasPreviousPets: true,
            "Ready to adopt",
            utcNow);

        adoptionRequest.Approve(reviewerId, "Approved", utcNow.AddMinutes(5));
        adoptionRequest.Complete(reviewerId, "Completed", utcNow.AddMinutes(15));

        adoptionRequest.Status.Should().Be(AdoptionRequestStatus.Completed);
        adoptionRequest.HasActiveStatus().Should().BeFalse();
        adoptionRequest.CanComplete().Should().BeFalse();
        adoptionRequest.CanApprove().Should().BeFalse();
        adoptionRequest.CanReject().Should().BeFalse();
    }
}
