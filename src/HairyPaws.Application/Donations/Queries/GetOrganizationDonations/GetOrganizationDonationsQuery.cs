using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Donations.Common;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Donations.Responses;
using HairyPaws.Domain.Identity.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Donations.Queries.GetOrganizationDonations;

public sealed record GetOrganizationDonationsQuery(
    int Page,
    int PageSize,
    string? Status,
    string? DonationType,
    string? Search,
    string? SortBy,
    string? SortDirection);

public sealed class GetOrganizationDonationsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetOrganizationDonationsQuery, PagedResponse<DonationListItemResponse>>
{
    public async Task<PagedResponse<DonationListItemResponse>> Handle(GetOrganizationDonationsQuery query, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);

        IQueryable<Domain.Donations.Entities.Donation> donations = dbContext.Donations
            .AsNoTracking()
            .IncludeForList();

        if (actor.Role == UserRole.Admin)
        {
            // Admin can view all organization donations.
        }
        else if (actor.Role == UserRole.Ong)
        {
            var organizationId = await CurrentUserContext.GetOwnedOrganizationIdAsync(dbContext, actor.Id, cancellationToken);
            if (!organizationId.HasValue)
            {
                return new PagedResponse<DonationListItemResponse>([], query.Page, query.PageSize, 0, 0);
            }

            donations = donations.Where(entity => entity.OrganizationId == organizationId.Value);
        }
        else
        {
            throw new ForbiddenAppException("Only organization owners or admins can access organization donations.");
        }

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

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            donations = donations.Where(entity =>
                entity.DonorUser.Email.Contains(search) ||
                entity.DonorUser.FirstName.ToLower().Contains(search) ||
                entity.DonorUser.LastName.ToLower().Contains(search) ||
                (entity.DonorUser.FirstName + " " + entity.DonorUser.LastName).ToLower().Contains(search));
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
            "donor" => descending
                ? donations.OrderByDescending(entity => entity.DonorUser.FirstName).ThenByDescending(entity => entity.DonorUser.LastName)
                : donations.OrderBy(entity => entity.DonorUser.FirstName).ThenBy(entity => entity.DonorUser.LastName),
            _ => descending
                ? donations.OrderByDescending(entity => entity.CreatedAt)
                : donations.OrderBy(entity => entity.CreatedAt)
        };
    }
}
