using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Organizations.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Organizations.Queries.GetMyOrganization;

public sealed record GetMyOrganizationQuery;

public sealed class GetMyOrganizationQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetMyOrganizationQuery, OrganizationDetailResponse>
{
    public async Task<OrganizationDetailResponse> Handle(GetMyOrganizationQuery query, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);

        var organization = await dbContext.Organizations
            .Include(entity => entity.Documents)
            .SingleOrDefaultAsync(
                entity => entity.OwnerUserId == actor.Id && entity.DeletedAt == null,
                cancellationToken)
            ?? throw new NotFoundException("The current user does not own an organization.");

        return organization.ToDetailResponse(includeDocuments: true);
    }
}
