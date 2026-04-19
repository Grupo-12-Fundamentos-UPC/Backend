using HairyPaws.Application.Adoption.Common;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Adoption.Responses;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Domain.Adoption.Entities;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Adoption.Queries.GetPetAdoptionRequests;

public sealed record GetPetAdoptionRequestsQuery(
    Guid PetId,
    int Page,
    int PageSize,
    string? Status,
    string? Search,
    string? SortBy,
    string? SortDirection);

public sealed class GetPetAdoptionRequestsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetPetAdoptionRequestsQuery, PagedResponse<AdoptionRequestListItemResponse>>
{
    public async Task<PagedResponse<AdoptionRequestListItemResponse>> Handle(GetPetAdoptionRequestsQuery query, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var pet = await dbContext.Pets
            .SingleOrDefaultAsync(entity => entity.Id == query.PetId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The pet was not found.");

        if (!await CurrentUserContext.CanManagePetAsync(dbContext, actor, pet, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to review adoption requests for this pet.");
        }

        var adoptionRequests = dbContext.AdoptionRequests
            .AsNoTracking()
            .IncludeForList()
            .Where(adoptionRequest => adoptionRequest.PetId == query.PetId);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = ContractEnumMapper.ToAdoptionRequestStatus(query.Status);
            adoptionRequests = adoptionRequests.Where(adoptionRequest => adoptionRequest.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            adoptionRequests = adoptionRequests.Where(adoptionRequest =>
                adoptionRequest.AdopterUser.Email.Contains(search) ||
                adoptionRequest.AdopterUser.FirstName.ToLower().Contains(search) ||
                adoptionRequest.AdopterUser.LastName.ToLower().Contains(search) ||
                (adoptionRequest.AdopterUser.FirstName + " " + adoptionRequest.AdopterUser.LastName).ToLower().Contains(search));
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
            "adopter" => descending
                ? query.OrderByDescending(entity => entity.AdopterUser.FirstName).ThenByDescending(entity => entity.AdopterUser.LastName)
                : query.OrderBy(entity => entity.AdopterUser.FirstName).ThenBy(entity => entity.AdopterUser.LastName),
            _ => descending
                ? query.OrderByDescending(entity => entity.CreatedAt)
                : query.OrderBy(entity => entity.CreatedAt)
        };
    }
}
