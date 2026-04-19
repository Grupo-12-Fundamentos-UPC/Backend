using HairyPaws.Contracts.Organizations.Responses;
using HairyPaws.Contracts.Users.Responses;

namespace HairyPaws.Contracts.Donations.Responses;

public sealed record DonationListItemResponse(
    Guid Id,
    UserSummaryResponse Donor,
    OrganizationSummaryResponse Organization,
    string DonationType,
    string Status,
    decimal? Amount,
    string? ReceiptPath,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
