using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Events.Common;
using HairyPaws.Contracts.Events.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Events.Queries.GetEventById;

public sealed record GetEventByIdQuery(Guid EventId);

public sealed class GetEventByIdQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetEventByIdQuery, EventDetailResponse>
{
    public async Task<EventDetailResponse> Handle(GetEventByIdQuery query, CancellationToken cancellationToken)
    {
        var eventEntity = await dbContext.Events
            .AsNoTracking()
            .IncludeForResponse()
            .SingleOrDefaultAsync(entity => entity.Id == query.EventId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The event was not found.");

        if (eventEntity.IsPubliclyVisible())
        {
            return eventEntity.ToDetailResponse();
        }

        if (!currentUserService.IsAuthenticated)
        {
            throw new NotFoundException("The event was not found.");
        }

        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        if (!await CurrentUserContext.CanManageEventAsync(dbContext, actor, eventEntity, cancellationToken))
        {
            throw new NotFoundException("The event was not found.");
        }

        return eventEntity.ToDetailResponse();
    }
}
