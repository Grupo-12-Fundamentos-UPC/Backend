using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Events.Common;
using HairyPaws.Contracts.Events.Responses;
using HairyPaws.Domain.Identity.Enums;
using HairyPaws.Domain.Notifications.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Events.Commands.PublishEvent;

public sealed record PublishEventCommand(Guid EventId);

public sealed class PublishEventCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    INotificationService notificationService,
    IAuditService auditService)
    : ICommandHandler<PublishEventCommand, EventDetailResponse>
{
    public async Task<EventDetailResponse> Handle(PublishEventCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var eventEntity = await dbContext.Events
            .IncludeForResponse()
            .SingleOrDefaultAsync(entity => entity.Id == command.EventId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The event was not found.");

        if (!await CurrentUserContext.CanManageEventAsync(dbContext, actor, eventEntity, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to publish this event.");
        }

        if (!eventEntity.CanPublish())
        {
            throw new BusinessRuleViolationException("Only draft events can be published.");
        }

        if (eventEntity.Organization.VerificationStatus != HairyPaws.Domain.Identity.Enums.VerificationStatus.Verified)
        {
            throw new BusinessRuleViolationException("Only verified organizations can publish events.");
        }

        var validationErrors = eventEntity.GetPublishValidationErrors(dateTimeProvider.UtcNow);
        if (validationErrors.Count > 0)
        {
            throw new BusinessRuleViolationException("The event does not satisfy the minimum publish requirements.", validationErrors);
        }

        var before = eventEntity.ToAuditSnapshot();
        eventEntity.Publish(dateTimeProvider.UtcNow);

        if (actor.Role == UserRole.Admin && eventEntity.Organization.OwnerUserId != actor.Id)
        {
            await notificationService.CreateAsync(
                eventEntity.Organization.OwnerUserId,
                NotificationType.EventPublished,
                "Event published",
                $"Your event '{eventEntity.Title}' was published.",
                "Event",
                eventEntity.Id,
                cancellationToken);
        }

        await auditService.WriteAsync(
            "Publish",
            actor.Id,
            "Event",
            eventEntity.Id,
            before,
            eventEntity.ToAuditSnapshot(),
            new
            {
                organizationVerificationStatus = eventEntity.Organization.VerificationStatus.ToString()
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
