using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Pets.Responses;
using HairyPaws.Domain.Adoption.Enums;
using HairyPaws.Domain.Pets.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Pets.Commands.ArchivePet;

public sealed record ArchivePetCommand(Guid PetId);

public sealed class ArchivePetCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<ArchivePetCommand, PetDetailResponse>
{
    public async Task<PetDetailResponse> Handle(ArchivePetCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var pet = await dbContext.Pets
            .Include(entity => entity.Photos)
            .SingleOrDefaultAsync(entity => entity.Id == command.PetId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The pet was not found.");

        if (!await CurrentUserContext.CanManagePetAsync(dbContext, actor, pet, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to archive this pet.");
        }

        if (pet.Status is not (PetStatus.Draft or PetStatus.Available))
        {
            throw new BusinessRuleViolationException("Only draft or available pets can be archived.");
        }

        if (pet.Status == PetStatus.Available)
        {
            var hasActiveAdoptionRequests = await dbContext.AdoptionRequests.AnyAsync(
                adoptionRequest =>
                    adoptionRequest.PetId == pet.Id &&
                    (adoptionRequest.Status == AdoptionRequestStatus.Submitted ||
                     adoptionRequest.Status == AdoptionRequestStatus.UnderReview ||
                     adoptionRequest.Status == AdoptionRequestStatus.Approved),
                cancellationToken);

            if (hasActiveAdoptionRequests)
            {
                throw new BusinessRuleViolationException("Pets with active adoption requests cannot be archived.");
            }
        }

        var before = pet.ToAuditSnapshot();
        pet.Archive(dateTimeProvider.UtcNow);
        await auditService.WriteAsync(
            "Archive",
            actor.Id,
            "Pet",
            pet.Id,
            before,
            pet.ToAuditSnapshot(),
            metadata: null,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return pet.ToDetailResponse();
    }
}
