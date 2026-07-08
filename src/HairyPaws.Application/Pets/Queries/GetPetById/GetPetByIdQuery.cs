using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Pets.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Pets.Queries.GetPetById;

public sealed record GetPetByIdQuery(Guid PetId);

public sealed class GetPetByIdQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetPetByIdQuery, PetDetailResponse>
{
    public async Task<PetDetailResponse> Handle(GetPetByIdQuery query, CancellationToken cancellationToken)
    {
        var pet = await dbContext.Pets
            .Include(entity => entity.Photos)
            .SingleOrDefaultAsync(entity => entity.Id == query.PetId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The pet was not found.");

        if (pet.IsPubliclyVisible())
        {
            return pet.ToDetailResponse();
        }

        if (!currentUserService.IsAuthenticated)
        {
            throw new NotFoundException("The pet was not found.");
        }

        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        if (!await CurrentUserContext.CanManagePetAsync(dbContext, actor, pet, cancellationToken))
        {
            throw new NotFoundException("The pet was not found.");
        }

        return pet.ToDetailResponse();
    }
}
