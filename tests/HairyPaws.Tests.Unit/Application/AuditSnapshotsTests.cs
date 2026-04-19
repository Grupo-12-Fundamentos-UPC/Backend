using System.Text.Json;
using FluentAssertions;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Domain.Donations.Entities;
using HairyPaws.Domain.Donations.Enums;
using HairyPaws.Domain.Identity.Entities;
using HairyPaws.Domain.Identity.Enums;

namespace HairyPaws.Tests.Unit.Application;

public sealed class AuditSnapshotsTests
{
    [Fact]
    public void UserSnapshot_ShouldSerializeReadableEnumValues()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var user = User.Create(
            "snapshot-user@hairypaws.test",
            "hashed-password",
            "Jane",
            "Doe",
            UserRole.Adopter,
            utcNow,
            phoneNumber: "5551234",
            identityDocument: "12345678",
            address: "Example Street");

        user.UpdateStatus(UserStatus.Suspended, utcNow.AddMinutes(1));
        user.UpdateVerificationStatus(VerificationStatus.Verified, utcNow.AddMinutes(2));

        var json = JsonSerializer.Serialize(user.ToAuditSnapshot());

        json.Should().Contain("\"Email\":\"snapshot-user@hairypaws.test\"");
        json.Should().Contain("\"Status\":\"Suspended\"");
        json.Should().Contain("\"VerificationStatus\":\"Verified\"");
        json.Should().Contain("\"Role\":\"Adopter\"");
    }

    [Fact]
    public void DonationSnapshot_ShouldIncludeReceiptAndConfirmationState()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var donation = Donation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DonationType.Money,
            35.5m,
            "txn-123",
            "Snapshot donation",
            utcNow);

        donation.ReplaceReceipt("/uploads/donations/receipts/example.pdf", utcNow.AddMinutes(1));
        donation.Confirm(Guid.NewGuid(), utcNow.AddMinutes(2));

        var json = JsonSerializer.Serialize(donation.ToAuditSnapshot());

        json.Should().Contain("\"Status\":\"Confirmed\"");
        json.Should().Contain("\"DonationType\":\"Money\"");
        json.Should().Contain("\"ReceiptPath\":\"/uploads/donations/receipts/example.pdf\"");
        json.Should().Contain("\"Amount\":35.5");
    }
}
