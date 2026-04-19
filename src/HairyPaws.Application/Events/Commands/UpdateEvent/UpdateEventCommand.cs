using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Events.Common;
using HairyPaws.Contracts.Events.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Events.Commands.UpdateEvent;

public sealed record UpdateEventCommand(
    Guid EventId,
    string? Title,
    string? Description,
    DateTimeOffset? EventDate,
    string? Location,
    bool? IsVolunteerEvent);

public sealed class UpdateEventCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<UpdateEventCommand, EventDetailResponse>
{
    public async Task<EventDetailResponse> Handle(UpdateEventCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var eventEntity = await dbContext.Events
            .IncludeForResponse()
            .SingleOrDefaultAsync(entity => entity.Id == command.EventId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The event was not found.");

        if (!await CurrentUserContext.CanManageEventAsync(dbContext, actor, eventEntity, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to update this event.");
        }

        if (!eventEntity.CanUpdate())
        {
            throw new BusinessRuleViolationException("Cancelled events cannot be updated.");
        }

        var title = command.Title ?? eventEntity.Title;
        var description = command.Description ?? eventEntity.Description;
        var eventDate = command.EventDate ?? eventEntity.EventDate;
        var location = command.Location ?? eventEntity.Location;
        var isVolunteerEvent = command.IsVolunteerEvent ?? eventEntity.IsVolunteerEvent;

        if (eventDate <= dateTimeProvider.UtcNow)
        {
            throw new BusinessRuleViolationException("Event date must be in the future.");
        }

        var before = eventEntity.ToAuditSnapshot();
        eventEntity.Update(title, description, eventDate, location, isVolunteerEvent, dateTimeProvider.UtcNow);
        await auditService.WriteAsync(
            "Update",
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
