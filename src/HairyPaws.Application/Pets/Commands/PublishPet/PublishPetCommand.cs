using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Pets.Responses;
using HairyPaws.Domain.Pets.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Pets.Commands.PublishPet;

public sealed record PublishPetCommand(Guid PetId);

public sealed class PublishPetCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<PublishPetCommand, PetDetailResponse>
{
    public async Task<PetDetailResponse> Handle(PublishPetCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var pet = await dbContext.Pets
            .Include(entity => entity.Photos)
            .SingleOrDefaultAsync(entity => entity.Id == command.PetId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The pet was not found.");

        if (!await CurrentUserContext.CanManagePetAsync(dbContext, actor, pet, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to publish this pet.");
        }

        if (pet.Status != PetStatus.Draft)
        {
            throw new BusinessRuleViolationException("Only draft pets can be published.");
        }

        var publishErrors = pet.GetPublishValidationErrors(pet.Photos.Count);
        if (publishErrors.Count > 0)
        {
            throw new BusinessRuleViolationException(
                "The pet does not meet the minimum publish requirements.",
                new { errors = publishErrors });
        }

        var before = pet.ToAuditSnapshot();
        pet.Publish(dateTimeProvider.UtcNow);
        await auditService.WriteAsync(
            "Publish",
            actor.Id,
            "Pet",
            pet.Id,
            before,
            pet.ToAuditSnapshot(),
            new
            {
                photoCount = pet.Photos.Count
            },
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return pet.ToDetailResponse();
    }
}
