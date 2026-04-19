namespace HairyPaws.Contracts.Donations.Requests;

public sealed record CreateDonationItemRequest
{
    public string Name { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public string? Description { get; init; }
}
