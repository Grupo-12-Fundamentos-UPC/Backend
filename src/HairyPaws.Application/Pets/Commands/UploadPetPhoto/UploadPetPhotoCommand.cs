using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Files;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Pets.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Pets.Commands.UploadPetPhoto;

public sealed record UploadPetPhotoCommand(Guid PetId, UploadedFile File);

public sealed class UploadPetPhotoCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IFileStorageService fileStorageService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<UploadPetPhotoCommand, PetPhotoResponse>
{
    private const long MaxPhotoSizeBytes = 5 * 1024 * 1024;

    public async Task<PetPhotoResponse> Handle(UploadPetPhotoCommand command, CancellationToken cancellationToken)
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

        UploadedFileValidator.EnsureImageIsValid(command.File, "file", MaxPhotoSizeBytes);
        var extension = UploadedFileValidator.GetRequiredExtension(command.File, "file", ".jpg", ".jpeg", ".png");
        var sortOrder = pet.Photos.Count == 0 ? 1 : pet.Photos.Max(entity => entity.SortOrder) + 1;
        var relativePath = $"pets/photos/{pet.Id}/{Guid.NewGuid():N}{extension}";

        await using var contentStream = command.File.OpenReadStream();
        var savedPath = await fileStorageService.SaveAsync(contentStream, relativePath, cancellationToken);

        var before = pet.ToAuditSnapshot();
        var photo = pet.AddPhoto(savedPath, sortOrder, dateTimeProvider.UtcNow);
        await dbContext.PetPhotos.AddAsync(photo, cancellationToken);
        await auditService.WriteAsync(
            "UploadPhoto",
            actor.Id,
            "Pet",
            pet.Id,
            before,
            pet.ToAuditSnapshot(),
            photo.ToAuditSnapshot(),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return photo.ToResponse();
    }
}
