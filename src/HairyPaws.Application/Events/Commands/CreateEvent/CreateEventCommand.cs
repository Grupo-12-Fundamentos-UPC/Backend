using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Events.Common;
using HairyPaws.Contracts.Events.Responses;
using HairyPaws.Domain.Events.Entities;
using HairyPaws.Domain.Identity.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Events.Commands.CreateEvent;

public sealed record CreateEventCommand(
    Guid OrganizationId,
    string Title,
    string Description,
    DateTimeOffset EventDate,
    string? Location,
    bool IsVolunteerEvent);

public sealed class CreateEventCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<CreateEventCommand, EventDetailResponse>
{
    public async Task<EventDetailResponse> Handle(CreateEventCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        if (actor.Role is not (UserRole.Ong or UserRole.Admin))
        {
            throw new ForbiddenAppException("Only organization owners or admins can create events.");
        }

        var organization = await dbContext.Organizations
            .SingleOrDefaultAsync(entity => entity.Id == command.OrganizationId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The organization was not found.");

        if (!await CurrentUserContext.CanManageOrganizationAsync(dbContext, actor, organization, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to create events for this organization.");
        }

        if (command.EventDate <= dateTimeProvider.UtcNow)
        {
            throw new BusinessRuleViolationException("Event date must be in the future.");
        }

        var eventEntity = Event.Create(
            organization.Id,
            command.Title,
            command.Description,
            command.EventDate,
            command.Location,
            command.IsVolunteerEvent,
            dateTimeProvider.UtcNow);

        await dbContext.Events.AddAsync(eventEntity, cancellationToken);
        await auditService.WriteAsync(
            "Create",
            actor.Id,
            "Event",
            eventEntity.Id,
            before: null,
            after: eventEntity.ToAuditSnapshot(),
            metadata: new
            {
                eventEntity.OrganizationId
            },
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await dbContext.Events
            .AsNoTracking()
            .IncludeForResponse()
            .SingleAsync(entity => entity.Id == eventEntity.Id, cancellationToken);

        return response.ToDetailResponse();
    }
}
