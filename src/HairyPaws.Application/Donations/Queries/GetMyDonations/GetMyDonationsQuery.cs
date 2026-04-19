using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Donations.Common;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Donations.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Donations.Queries.GetMyDonations;

public sealed record GetMyDonationsQuery(
    int Page,
    int PageSize,
    string? Status,
    string? DonationType,
    string? SortBy,
    string? SortDirection);

public sealed class GetMyDonationsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetMyDonationsQuery, PagedResponse<DonationListItemResponse>>
{
    public async Task<PagedResponse<DonationListItemResponse>> Handle(GetMyDonationsQuery query, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);

        var donations = dbContext.Donations
            .AsNoTracking()
            .IncludeForList()
            .Where(entity => entity.DonorUserId == actor.Id);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = ContractEnumMapper.ToDonationStatus(query.Status);
            donations = donations.Where(entity => entity.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.DonationType))
        {
            var donationType = ContractEnumMapper.ToDonationType(query.DonationType);
            donations = donations.Where(entity => entity.DonationType == donationType);
        }

        donations = ApplySorting(donations, query.SortBy, query.SortDirection);

        var totalCount = await donations.LongCountAsync(cancellationToken);
        var items = await donations
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResponse<DonationListItemResponse>(
            items.Select(static entity => entity.ToListItemResponse()).ToArray(),
            query.Page,
            query.PageSize,
            totalCount,
            totalPages);
    }

    private static IQueryable<Domain.Donations.Entities.Donation> ApplySorting(
        IQueryable<Domain.Donations.Entities.Donation> donations,
        string? sortBy,
        string? sortDirection)
    {
        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();
        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return normalizedSortBy switch
        {
            "updatedat" => descending
                ? donations.OrderByDescending(entity => entity.UpdatedAt)
                : donations.OrderBy(entity => entity.UpdatedAt),
            "amount" => descending
                ? donations.OrderByDescending(entity => entity.Amount).ThenByDescending(entity => entity.CreatedAt)
                : donations.OrderBy(entity => entity.Amount).ThenBy(entity => entity.CreatedAt),
            "status" => descending
                ? donations.OrderByDescending(entity => entity.Status).ThenByDescending(entity => entity.CreatedAt)
                : donations.OrderBy(entity => entity.Status).ThenBy(entity => entity.CreatedAt),
            _ => descending
                ? donations.OrderByDescending(entity => entity.CreatedAt)
                : donations.OrderBy(entity => entity.CreatedAt)
        };
    }
}
