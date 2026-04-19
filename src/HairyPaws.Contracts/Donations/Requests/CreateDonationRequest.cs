namespace HairyPaws.Contracts.Donations.Requests;

public sealed record CreateDonationRequest
{
    public Guid OrganizationId { get; init; }

    public string DonationType { get; init; } = string.Empty;

    public decimal? Amount { get; init; }

    public string? TransactionId { get; init; }

    public string? Notes { get; init; }

    public IReadOnlyCollection<CreateDonationItemRequest> Items { get; init; } = [];
}
