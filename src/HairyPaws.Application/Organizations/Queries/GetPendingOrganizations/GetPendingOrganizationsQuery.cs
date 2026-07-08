using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Organizations.Responses;
using HairyPaws.Domain.Identity.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Organizations.Queries.GetPendingOrganizations;

public sealed record GetPendingOrganizationsQuery(int Page, int PageSize, string? Search);

public sealed class GetPendingOrganizationsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetPendingOrganizationsQuery, PagedResponse<OrganizationSummaryResponse>>
{
    public async Task<PagedResponse<OrganizationSummaryResponse>> Handle(GetPendingOrganizationsQuery query, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        if (actor.Role != UserRole.Admin)
        {
            throw new ForbiddenAppException("Only administrators can review organizations.");
        }

        var organizations = dbContext.Organizations
            .AsNoTracking()
            .Where(entity => entity.DeletedAt == null && entity.VerificationStatus == VerificationStatus.Pending);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            organizations = organizations.Where(entity =>
                entity.Name.ToLower().Contains(search) ||
                entity.Ruc.Contains(search) ||
                (entity.Email != null && entity.Email.ToLower().Contains(search)));
        }

        var totalCount = await organizations.LongCountAsync(cancellationToken);
        var items = await organizations
            .OrderBy(entity => entity.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize);

        return new PagedResponse<OrganizationSummaryResponse>(
            items.Select(static organization => organization.ToSummaryResponse()).ToArray(),
            query.Page,
            query.PageSize,
            totalCount,
            totalPages);
    }
}
