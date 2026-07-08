using HairyPaws.Application.Adoption.Common;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Adoption.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Adoption.Queries.GetAdoptionRequestById;

public sealed record GetAdoptionRequestByIdQuery(Guid AdoptionRequestId);

public sealed class GetAdoptionRequestByIdQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetAdoptionRequestByIdQuery, AdoptionRequestDetailResponse>
{
    public async Task<AdoptionRequestDetailResponse> Handle(GetAdoptionRequestByIdQuery query, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var adoptionRequest = await dbContext.AdoptionRequests
            .AsNoTracking()
            .IncludeForDetail()
            .SingleOrDefaultAsync(entity => entity.Id == query.AdoptionRequestId, cancellationToken)
            ?? throw new NotFoundException("The adoption request was not found.");

        if (!await CurrentUserContext.CanAccessAdoptionRequestAsync(dbContext, actor, adoptionRequest, cancellationToken))
        {
            throw new NotFoundException("The adoption request was not found.");
        }

        return adoptionRequest.ToDetailResponse();
    }
}
