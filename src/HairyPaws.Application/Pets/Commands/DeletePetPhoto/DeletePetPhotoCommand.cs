using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Security;
using HairyPaws.Domain.Pets.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Pets.Commands.DeletePetPhoto;

public sealed record DeletePetPhotoCommand(Guid PetId, Guid PhotoId);

public sealed class DeletePetPhotoCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IFileStorageService fileStorageService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<DeletePetPhotoCommand>
{
    public async Task Handle(DeletePetPhotoCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var pet = await dbContext.Pets
            .Include(entity => entity.Photos)
            .SingleOrDefaultAsync(entity => entity.Id == command.PetId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The pet was not found.");

        if (!await CurrentUserContext.CanManagePetAsync(dbContext, actor, pet, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to manage this pet's photos.");
        }

        var photo = pet.Photos.SingleOrDefault(entity => entity.Id == command.PhotoId)
            ?? throw new NotFoundException("The pet photo was not found.");

        if (pet.Status == PetStatus.Available && pet.Photos.Count <= 1)
        {
            throw new BusinessRuleViolationException("Available pets must keep at least one photo.");
        }

        var before = pet.ToAuditSnapshot();
        var removedPhoto = photo.ToAuditSnapshot();
        pet.RemovePhoto(photo, dateTimeProvider.UtcNow);
        dbContext.PetPhotos.Remove(photo);
        await auditService.WriteAsync(
            "DeletePhoto",
            actor.Id,
            "Pet",
            pet.Id,
            before,
            pet.ToAuditSnapshot(),
            removedPhoto,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await fileStorageService.DeleteAsync(photo.FilePath, cancellationToken);
    }
}
