using HairyPaws.Domain.Common.Abstractions;

namespace HairyPaws.Domain.Donations.Entities;

public sealed class DonationItem : Entity
{
    private DonationItem()
    {
    }

    private DonationItem(
        Guid donationId,
        string name,
        int quantity,
        string? description,
        DateTimeOffset utcNow)
    {
        Id = Guid.NewGuid();
        DonationId = donationId;
        Name = NormalizeRequired(name);
        Quantity = quantity;
        Description = NormalizeOptional(description);
        CreatedAt = utcNow;
    }

    public Guid DonationId { get; private set; }

    public Donation Donation { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public string? Description { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static DonationItem Create(
        Guid donationId,
        string name,
        int quantity,
        string? description,
        DateTimeOffset utcNow)
    {
        return new DonationItem(donationId, name, quantity, description, utcNow);
    }

    private static string NormalizeRequired(string value) => value.Trim();

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
