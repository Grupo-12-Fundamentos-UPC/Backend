using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Pets.Responses;
using HairyPaws.Domain.Identity.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Pets.Queries.GetMyPets;

public sealed record GetMyPetsQuery;

public sealed class GetMyPetsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetMyPetsQuery, IReadOnlyCollection<PetListItemResponse>>
{
    public async Task<IReadOnlyCollection<PetListItemResponse>> Handle(GetMyPetsQuery query, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var pets = dbContext.Pets
            .AsNoTracking()
            .Include(entity => entity.Photos)
            .Where(entity => entity.DeletedAt == null);

        pets = actor.Role switch
        {
            UserRole.Owner => pets.Where(entity => entity.OwnerUserId == actor.Id),
            UserRole.Ong => await FilterOrganizationPetsAsync(pets, actor.Id, cancellationToken),
            _ => throw new ForbiddenAppException("Only users with role Owner or Ong can access their pets.")
        };

        var items = await pets
            .OrderByDescending(entity => entity.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(static pet => pet.ToListItemResponse()).ToArray();
    }

    private async Task<IQueryable<HairyPaws.Domain.Pets.Entities.Pet>> FilterOrganizationPetsAsync(
        IQueryable<HairyPaws.Domain.Pets.Entities.Pet> pets,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var organizationId = await CurrentUserContext.GetOwnedOrganizationIdAsync(dbContext, actorId, cancellationToken);
        return organizationId.HasValue
            ? pets.Where(entity => entity.OrganizationId == organizationId.Value)
            : pets.Where(static _ => false);
    }
}
