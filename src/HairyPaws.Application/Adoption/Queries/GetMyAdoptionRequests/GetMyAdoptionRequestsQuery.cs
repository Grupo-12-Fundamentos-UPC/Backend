using HairyPaws.Application.Adoption.Common;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Adoption.Responses;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Domain.Adoption.Entities;
using HairyPaws.Domain.Identity.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Adoption.Queries.GetMyAdoptionRequests;

public sealed record GetMyAdoptionRequestsQuery(
    int Page,
    int PageSize,
    string? Status,
    string? SortBy,
    string? SortDirection);

public sealed class GetMyAdoptionRequestsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetMyAdoptionRequestsQuery, PagedResponse<AdoptionRequestListItemResponse>>
{
    public async Task<PagedResponse<AdoptionRequestListItemResponse>> Handle(GetMyAdoptionRequestsQuery query, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        if (actor.Role != UserRole.Adopter)
        {
            throw new ForbiddenAppException("Only users with role Adopter can access their adoption requests.");
        }

        var adoptionRequests = dbContext.AdoptionRequests
            .AsNoTracking()
            .IncludeForList()
            .Where(adoptionRequest => adoptionRequest.AdopterUserId == actor.Id);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = ContractEnumMapper.ToAdoptionRequestStatus(query.Status);
            adoptionRequests = adoptionRequests.Where(adoptionRequest => adoptionRequest.Status == status);
        }

        adoptionRequests = ApplySorting(adoptionRequests, query.SortBy, query.SortDirection);

        var totalCount = await adoptionRequests.LongCountAsync(cancellationToken);
        var items = await adoptionRequests
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize);

        return new PagedResponse<AdoptionRequestListItemResponse>(
            items.Select(static adoptionRequest => adoptionRequest.ToListItemResponse()).ToArray(),
            query.Page,
            query.PageSize,
            totalCount,
            totalPages);
    }

    private static IQueryable<AdoptionRequest> ApplySorting(
        IQueryable<AdoptionRequest> query,
        string? sortBy,
        string? sortDirection)
    {
        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();
        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return normalizedSortBy switch
        {
            "updatedat" => descending
                ? query.OrderByDescending(entity => entity.UpdatedAt)
                : query.OrderBy(entity => entity.UpdatedAt),
            "status" => descending
                ? query.OrderByDescending(entity => entity.Status).ThenByDescending(entity => entity.CreatedAt)
                : query.OrderBy(entity => entity.Status).ThenBy(entity => entity.CreatedAt),
            _ => descending
                ? query.OrderByDescending(entity => entity.CreatedAt)
                : query.OrderBy(entity => entity.CreatedAt)
        };
    }
}
