using HairyPaws.Contracts.Donations.Responses;
using HairyPaws.Domain.Donations.Entities;

namespace HairyPaws.Application.Common.Mappings;

public static class DonationResponseMappings
{
    public static DonationListItemResponse ToListItemResponse(this Donation donation)
    {
        return new DonationListItemResponse(
            donation.Id,
            donation.DonorUser.ToSummaryResponse(),
            donation.Organization.ToSummaryResponse(),
            donation.DonationType.ToString(),
            donation.Status.ToString(),
            donation.Amount,
            donation.ReceiptPath,
            donation.CreatedAt,
            donation.UpdatedAt);
    }

    public static DonationResponse ToResponse(this Donation donation)
    {
        return new DonationResponse(
            donation.Id,
            donation.DonorUser.ToSummaryResponse(),
            donation.Organization.ToSummaryResponse(),
            donation.DonationType.ToString(),
            donation.Status.ToString(),
            donation.Amount,
            donation.TransactionId,
            donation.Notes,
            donation.ReceiptPath,
            donation.ConfirmedByUser?.ToSummaryResponse(),
            donation.ConfirmedAt,
            donation.Items
                .OrderBy(item => item.CreatedAt)
                .Select(static item => item.ToResponse())
                .ToArray(),
            donation.CreatedAt,
            donation.UpdatedAt);
    }

    public static DonationItemResponse ToResponse(this DonationItem item)
    {
        return new DonationItemResponse(
            item.Id,
            item.Name,
            item.Quantity,
            item.Description,
            item.CreatedAt);
    }
}
