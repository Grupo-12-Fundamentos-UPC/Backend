using HairyPaws.Domain.Donations.Entities;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Donations.Common;

internal static class DonationQueryExtensions
{
    public static IQueryable<Donation> IncludeForList(this IQueryable<Donation> query)
    {
        return query
            .Include(donation => donation.DonorUser)
            .Include(donation => donation.Organization);
    }

    public static IQueryable<Donation> IncludeForDetail(this IQueryable<Donation> query)
    {
        return query
            .IncludeForList()
            .Include(donation => donation.ConfirmedByUser)
            .Include(donation => donation.Items);
    }
}
