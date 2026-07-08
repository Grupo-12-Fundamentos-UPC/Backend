using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Events.Common;
using HairyPaws.Contracts.Events.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Events.Commands.CancelEvent;

public sealed record CancelEventCommand(Guid EventId);

public sealed class CancelEventCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<CancelEventCommand, EventDetailResponse>
{
    public async Task<EventDetailResponse> Handle(CancelEventCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var eventEntity = await dbContext.Events
            .IncludeForResponse()
            .SingleOrDefaultAsync(entity => entity.Id == command.EventId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The event was not found.");

        if (!await CurrentUserContext.CanManageEventAsync(dbContext, actor, eventEntity, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to cancel this event.");
        }

        if (!eventEntity.CanCancel())
        {
            throw new BusinessRuleViolationException("Only draft or published events can be cancelled.");
        }

        var before = eventEntity.ToAuditSnapshot();
        eventEntity.Cancel(dateTimeProvider.UtcNow);
        await auditService.WriteAsync(
            "Cancel",
            actor.Id,
            "Event",
            eventEntity.Id,
            before,
            eventEntity.ToAuditSnapshot(),
            metadata: null,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await dbContext.Events
            .AsNoTracking()
            .IncludeForResponse()
            .SingleAsync(entity => entity.Id == eventEntity.Id, cancellationToken);

        return response.ToDetailResponse();
    }
}
