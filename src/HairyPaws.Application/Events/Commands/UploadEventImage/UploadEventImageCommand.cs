using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Files;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Events.Common;
using HairyPaws.Contracts.Events.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Events.Commands.UploadEventImage;

public sealed record UploadEventImageCommand(Guid EventId, UploadedFile File);

public sealed class UploadEventImageCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IFileStorageService fileStorageService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<UploadEventImageCommand, EventDetailResponse>
{
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;

    public async Task<EventDetailResponse> Handle(UploadEventImageCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var eventEntity = await dbContext.Events
            .IncludeForResponse()
            .SingleOrDefaultAsync(entity => entity.Id == command.EventId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The event was not found.");

        if (!await CurrentUserContext.CanManageEventAsync(dbContext, actor, eventEntity, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to manage this event image.");
        }

        if (!eventEntity.CanUpdate())
        {
            throw new BusinessRuleViolationException("Cancelled events cannot be updated.");
        }

        UploadedFileValidator.EnsureImageIsValid(command.File, "file", MaxImageSizeBytes);
        var extension = UploadedFileValidator.GetRequiredExtension(command.File, "file", ".jpg", ".jpeg", ".png");
        var relativePath = $"events/images/{eventEntity.Id}/{Guid.NewGuid():N}{extension}";
        var previousImagePath = eventEntity.ImagePath;
        var before = eventEntity.ToAuditSnapshot();

        if (!string.IsNullOrWhiteSpace(eventEntity.ImagePath))
        {
            await fileStorageService.DeleteAsync(eventEntity.ImagePath, cancellationToken);
        }

        await using var contentStream = command.File.OpenReadStream();
        var savedPath = await fileStorageService.SaveAsync(contentStream, relativePath, cancellationToken);

        eventEntity.SetImage(savedPath, dateTimeProvider.UtcNow);
        await auditService.WriteAsync(
            "UploadImage",
            actor.Id,
            "Event",
            eventEntity.Id,
            before,
            eventEntity.ToAuditSnapshot(),
            new
            {
                previousImagePath,
                savedPath
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
