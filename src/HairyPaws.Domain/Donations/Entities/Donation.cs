using HairyPaws.Domain.Common.Abstractions;
using HairyPaws.Domain.Donations.Enums;
using HairyPaws.Domain.Identity.Entities;
using HairyPaws.Domain.Organizations.Entities;

namespace HairyPaws.Domain.Donations.Entities;

public sealed class Donation : Entity
{
    private readonly List<DonationItem> _items = [];

    private Donation()
    {
    }

    private Donation(
        Guid donorUserId,
        Guid organizationId,
        DonationType donationType,
        decimal? amount,
        string? transactionId,
        string? notes,
        DateTimeOffset utcNow)
    {
        Id = Guid.NewGuid();
        DonorUserId = donorUserId;
        OrganizationId = organizationId;
        DonationType = donationType;
        Status = DonationStatus.Pending;
        Amount = amount;
        TransactionId = NormalizeOptional(transactionId);
        Notes = NormalizeOptional(notes);
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public Guid DonorUserId { get; private set; }

    public User DonorUser { get; private set; } = null!;

    public Guid OrganizationId { get; private set; }

    public Organization Organization { get; private set; } = null!;

    public DonationType DonationType { get; private set; }

    public DonationStatus Status { get; private set; }

    public decimal? Amount { get; private set; }

    public string? TransactionId { get; private set; }

    public string? Notes { get; private set; }

    public string? ReceiptPath { get; private set; }

    public Guid? ConfirmedByUserId { get; private set; }

    public User? ConfirmedByUser { get; private set; }

    public DateTimeOffset? ConfirmedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<DonationItem> Items => _items;

    public static Donation Create(
        Guid donorUserId,
        Guid organizationId,
        DonationType donationType,
        decimal? amount,
        string? transactionId,
        string? notes,
        DateTimeOffset utcNow)
    {
        return new Donation(donorUserId, organizationId, donationType, amount, transactionId, notes, utcNow);
    }

    public bool IsOwnedByDonor(Guid userId) => DonorUserId == userId;

    public bool BelongsToOrganization(Guid organizationId) => OrganizationId == organizationId;

    public bool CanConfirm() => Status == DonationStatus.Pending;

    public bool CanCancel() => Status == DonationStatus.Pending;

    public bool CanManageReceipt() => Status == DonationStatus.Pending;

    public DonationItem AddItem(string name, int quantity, string? description, DateTimeOffset utcNow)
    {
        var item = DonationItem.Create(Id, name, quantity, description, utcNow);
        _items.Add(item);
        UpdatedAt = utcNow;
        return item;
    }

    public void ReplaceReceipt(string? receiptPath, DateTimeOffset utcNow)
    {
        ReceiptPath = NormalizeOptional(receiptPath);
        UpdatedAt = utcNow;
    }

    public void Confirm(Guid confirmedByUserId, DateTimeOffset utcNow)
    {
        Status = DonationStatus.Confirmed;
        ConfirmedByUserId = confirmedByUserId;
        ConfirmedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void Cancel(DateTimeOffset utcNow)
    {
        Status = DonationStatus.Cancelled;
        UpdatedAt = utcNow;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
