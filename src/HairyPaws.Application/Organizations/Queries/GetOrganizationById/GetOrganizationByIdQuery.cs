using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Organizations.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Organizations.Queries.GetOrganizationById;

public sealed record GetOrganizationByIdQuery(Guid OrganizationId);

public sealed class GetOrganizationByIdQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetOrganizationByIdQuery, OrganizationDetailResponse>
{
    public async Task<OrganizationDetailResponse> Handle(GetOrganizationByIdQuery query, CancellationToken cancellationToken)
    {
        var organization = await dbContext.Organizations
            .Include(entity => entity.Documents)
            .SingleOrDefaultAsync(
                entity => entity.Id == query.OrganizationId && entity.DeletedAt == null,
                cancellationToken)
            ?? throw new NotFoundException("The organization was not found.");

        if (organization.IsVisibleToPublic())
        {
            return organization.ToDetailResponse(includeDocuments: false);
        }

        if (!currentUserService.IsAuthenticated)
        {
            throw new NotFoundException("The organization was not found.");
        }

        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        if (!await CurrentUserContext.CanManageOrganizationAsync(dbContext, actor, organization, cancellationToken) &&
            actor.Role != HairyPaws.Domain.Identity.Enums.UserRole.Admin)
        {
            throw new NotFoundException("The organization was not found.");
        }

        return organization.ToDetailResponse(includeDocuments: true);
    }
}
