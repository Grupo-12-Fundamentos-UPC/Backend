using FluentAssertions;
using HairyPaws.Domain.Donations.Entities;
using HairyPaws.Domain.Donations.Enums;

namespace HairyPaws.Tests.Unit.Domain;

public sealed class DonationTests
{
    [Fact]
    public void Create_ShouldStartAsPending()
    {
        var donation = Donation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DonationType.Money,
            50,
            "txn-123",
            "Initial donation",
            DateTimeOffset.UtcNow);

        donation.Status.Should().Be(DonationStatus.Pending);
        donation.CanConfirm().Should().BeTrue();
        donation.CanCancel().Should().BeTrue();
        donation.CanManageReceipt().Should().BeTrue();
    }

    [Fact]
    public void Confirm_ShouldSetConfirmedFieldsAndPreventFurtherPendingActions()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var donation = Donation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DonationType.Money,
            80,
            null,
            null,
            utcNow);

        donation.Confirm(Guid.NewGuid(), utcNow.AddMinutes(10));

        donation.Status.Should().Be(DonationStatus.Confirmed);
        donation.ConfirmedByUserId.Should().NotBeNull();
        donation.ConfirmedAt.Should().Be(utcNow.AddMinutes(10));
        donation.CanConfirm().Should().BeFalse();
        donation.CanCancel().Should().BeFalse();
        donation.CanManageReceipt().Should().BeFalse();
    }

    [Fact]
    public void AddItem_ShouldAppendItemAndUpdateTimestamp()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var donation = Donation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DonationType.Items,
            null,
            null,
            "Item donation",
            utcNow);

        var item = donation.AddItem("Blankets", 4, "Warm blankets", utcNow.AddMinutes(5));

        donation.Items.Should().ContainSingle();
        item.Name.Should().Be("Blankets");
        item.Quantity.Should().Be(4);
        donation.UpdatedAt.Should().Be(utcNow.AddMinutes(5));
    }
}
