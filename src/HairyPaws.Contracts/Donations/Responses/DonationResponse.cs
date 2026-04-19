using HairyPaws.Contracts.Organizations.Responses;
using HairyPaws.Contracts.Users.Responses;

namespace HairyPaws.Contracts.Donations.Responses;

public sealed record DonationResponse(
    Guid Id,
    UserSummaryResponse Donor,
    OrganizationSummaryResponse Organization,
    string DonationType,
    string Status,
    decimal? Amount,
    string? TransactionId,
    string? Notes,
    string? ReceiptPath,
    UserSummaryResponse? ConfirmedBy,
    DateTimeOffset? ConfirmedAt,
    IReadOnlyCollection<DonationItemResponse> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
