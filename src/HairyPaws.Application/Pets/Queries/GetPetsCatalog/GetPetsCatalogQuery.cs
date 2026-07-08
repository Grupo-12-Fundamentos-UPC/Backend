using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Pets.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Pets.Queries.GetPetsCatalog;

public sealed record GetPetsCatalogQuery(
    int Page,
    int PageSize,
    string? Species,
    string? Sex,
    string? Size,
    string? LocationDistrict,
    string? Search,
    string? SortBy,
    string? SortDirection);

public sealed class GetPetsCatalogQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetPetsCatalogQuery, PagedResponse<PetListItemResponse>>
{
    public async Task<PagedResponse<PetListItemResponse>> Handle(GetPetsCatalogQuery query, CancellationToken cancellationToken)
    {
        var pets = dbContext.Pets
            .AsNoTracking()
            .Include(entity => entity.Photos)
            .Where(entity => entity.DeletedAt == null && entity.Status == HairyPaws.Domain.Pets.Enums.PetStatus.Available);

        if (!string.IsNullOrWhiteSpace(query.Species))
        {
            var species = ContractEnumMapper.ToPetSpecies(query.Species);
            pets = pets.Where(entity => entity.Species == species);
        }

        if (!string.IsNullOrWhiteSpace(query.Sex))
        {
            var sex = ContractEnumMapper.ToPetSex(query.Sex);
            pets = pets.Where(entity => entity.Sex == sex);
        }

        if (!string.IsNullOrWhiteSpace(query.Size))
        {
            var size = ContractEnumMapper.ToPetSize(query.Size);
            pets = pets.Where(entity => entity.Size == size);
        }

        if (!string.IsNullOrWhiteSpace(query.LocationDistrict))
        {
            var locationDistrict = query.LocationDistrict.Trim().ToLowerInvariant();
            pets = pets.Where(entity => entity.LocationDistrict != null && entity.LocationDistrict.ToLower().Contains(locationDistrict));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            pets = pets.Where(entity =>
                (entity.Name != null && entity.Name.ToLower().Contains(search)) ||
                (entity.Breed != null && entity.Breed.ToLower().Contains(search)) ||
                (entity.Description != null && entity.Description.ToLower().Contains(search)));
        }

        pets = ApplySorting(pets, query.SortBy, query.SortDirection);

        var totalCount = await pets.LongCountAsync(cancellationToken);
        var items = await pets
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize);

        return new PagedResponse<PetListItemResponse>(
            items.Select(static pet => pet.ToListItemResponse()).ToArray(),
            query.Page,
            query.PageSize,
            totalCount,
            totalPages);
    }

    private static IQueryable<HairyPaws.Domain.Pets.Entities.Pet> ApplySorting(
        IQueryable<HairyPaws.Domain.Pets.Entities.Pet> pets,
        string? sortBy,
        string? sortDirection)
    {
        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();
        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return normalizedSortBy switch
        {
            "name" => descending
                ? pets.OrderByDescending(entity => entity.Name).ThenByDescending(entity => entity.PublishedAt)
                : pets.OrderBy(entity => entity.Name).ThenBy(entity => entity.PublishedAt),
            "createdat" => descending
                ? pets.OrderByDescending(entity => entity.CreatedAt)
                : pets.OrderBy(entity => entity.CreatedAt),
            _ => descending
                ? pets.OrderByDescending(entity => entity.PublishedAt).ThenByDescending(entity => entity.CreatedAt)
                : pets.OrderBy(entity => entity.PublishedAt).ThenBy(entity => entity.CreatedAt)
        };
    }
}
