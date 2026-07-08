using HairyPaws.Application.Adoption.Common;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Visits.Responses;
using HairyPaws.Domain.Visits.Entities;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Visits.Queries.GetVisitsByAdoptionRequest;

public sealed record GetVisitsByAdoptionRequestQuery(
    Guid AdoptionRequestId,
    int Page,
    int PageSize,
    string? Status,
    string? SortBy,
    string? SortDirection);

public sealed class GetVisitsByAdoptionRequestQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetVisitsByAdoptionRequestQuery, PagedResponse<VisitListItemResponse>>
{
    public async Task<PagedResponse<VisitListItemResponse>> Handle(GetVisitsByAdoptionRequestQuery query, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var adoptionRequest = await dbContext.AdoptionRequests
            .AsNoTracking()
            .IncludeForDetail()
            .SingleOrDefaultAsync(entity => entity.Id == query.AdoptionRequestId, cancellationToken)
            ?? throw new NotFoundException("The adoption request was not found.");

        if (!await CurrentUserContext.CanAccessAdoptionRequestAsync(dbContext, actor, adoptionRequest, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to access visits for this adoption request.");
        }

        var visits = dbContext.Visits
            .AsNoTracking()
            .Where(visit => visit.AdoptionRequestId == query.AdoptionRequestId);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = ContractEnumMapper.ToVisitStatus(query.Status);
            visits = visits.Where(visit => visit.Status == status);
        }

        visits = ApplySorting(visits, query.SortBy, query.SortDirection);

        var totalCount = await visits.LongCountAsync(cancellationToken);
        var items = await visits
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize);

        return new PagedResponse<VisitListItemResponse>(
            items.Select(static visit => visit.ToListItemResponse()).ToArray(),
            query.Page,
            query.PageSize,
            totalCount,
            totalPages);
    }

    private static IQueryable<Visit> ApplySorting(
        IQueryable<Visit> query,
        string? sortBy,
        string? sortDirection)
    {
        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();
        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return normalizedSortBy switch
        {
            "createdat" => descending
                ? query.OrderByDescending(entity => entity.CreatedAt)
                : query.OrderBy(entity => entity.CreatedAt),
            "status" => descending
                ? query.OrderByDescending(entity => entity.Status).ThenByDescending(entity => entity.ScheduledAt)
                : query.OrderBy(entity => entity.Status).ThenBy(entity => entity.ScheduledAt),
            _ => descending
                ? query.OrderByDescending(entity => entity.ScheduledAt)
                : query.OrderBy(entity => entity.ScheduledAt)
        };
    }
}
