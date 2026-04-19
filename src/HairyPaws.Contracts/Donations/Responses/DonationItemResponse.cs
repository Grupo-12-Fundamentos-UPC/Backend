namespace HairyPaws.Contracts.Donations.Responses;

public sealed record DonationItemResponse(
    Guid Id,
    string Name,
    int Quantity,
    string? Description,
    DateTimeOffset CreatedAt);
