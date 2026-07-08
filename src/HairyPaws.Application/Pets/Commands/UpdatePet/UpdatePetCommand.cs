using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Pets.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Pets.Commands.UpdatePet;

public sealed record UpdatePetCommand(
    Guid PetId,
    string? Name,
    string? Species,
    string? Breed,
    string? AgeText,
    string? Sex,
    string? Size,
    bool? Sterilized,
    bool? Vaccinated,
    string? Description,
    string? Temperament,
    string? MedicalHistory,
    string? LocationDistrict);

public sealed class UpdatePetCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<UpdatePetCommand, PetDetailResponse>
{
    public async Task<PetDetailResponse> Handle(UpdatePetCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var pet = await dbContext.Pets
            .Include(entity => entity.Photos)
            .SingleOrDefaultAsync(entity => entity.Id == command.PetId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The pet was not found.");

        if (!await CurrentUserContext.CanManagePetAsync(dbContext, actor, pet, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to update this pet.");
        }

        var before = pet.ToAuditSnapshot();
        pet.UpdateDetails(
            command.Name is null ? pet.Name : command.Name,
            string.IsNullOrWhiteSpace(command.Species) ? pet.Species : ContractEnumMapper.ToPetSpecies(command.Species),
            command.Breed is null ? pet.Breed : command.Breed,
            command.AgeText is null ? pet.AgeText : command.AgeText,
            string.IsNullOrWhiteSpace(command.Sex) ? pet.Sex : ContractEnumMapper.ToPetSex(command.Sex),
            string.IsNullOrWhiteSpace(command.Size) ? pet.Size : ContractEnumMapper.ToPetSize(command.Size),
            command.Sterilized ?? pet.Sterilized,
            command.Vaccinated ?? pet.Vaccinated,
            command.Description is null ? pet.Description : command.Description,
            command.Temperament is null ? pet.Temperament : command.Temperament,
            command.MedicalHistory is null ? pet.MedicalHistory : command.MedicalHistory,
            command.LocationDistrict is null ? pet.LocationDistrict : command.LocationDistrict,
            dateTimeProvider.UtcNow);

        if (pet.Status == HairyPaws.Domain.Pets.Enums.PetStatus.Available)
        {
            var publishErrors = pet.GetPublishValidationErrors(pet.Photos.Count);
            if (publishErrors.Count > 0)
            {
                throw new BusinessRuleViolationException(
                    "Available pets must continue to satisfy the publish requirements.",
                    new { errors = publishErrors });
            }
        }

        await auditService.WriteAsync(
            "Update",
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
