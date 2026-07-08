using HairyPaws.Application.Adoption.Common;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Visits.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Visits.Queries.GetVisitById;

public sealed record GetVisitByIdQuery(Guid VisitId);

public sealed class GetVisitByIdQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetVisitByIdQuery, VisitResponse>
{
    public async Task<VisitResponse> Handle(GetVisitByIdQuery query, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var visit = await dbContext.Visits
            .AsNoTracking()
            .IncludeForDetail()
            .SingleOrDefaultAsync(entity => entity.Id == query.VisitId, cancellationToken)
            ?? throw new NotFoundException("The visit was not found.");

        if (!await CurrentUserContext.CanAccessVisitAsync(dbContext, actor, visit, cancellationToken))
        {
            throw new NotFoundException("The visit was not found.");
        }

        return visit.ToResponse();
    }
}
