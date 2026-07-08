using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Pets.Responses;
using HairyPaws.Domain.Identity.Enums;
using HairyPaws.Domain.Pets.Entities;

namespace HairyPaws.Application.Pets.Commands.CreatePet;

public sealed record CreatePetCommand(
    string? Name,
    string Species,
    string? Breed,
    string? AgeText,
    string Sex,
    string Size,
    bool Sterilized,
    bool Vaccinated,
    string? Description,
    string? Temperament,
    string? MedicalHistory,
    string? LocationDistrict);

public sealed class CreatePetCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<CreatePetCommand, PetDetailResponse>
{
    public async Task<PetDetailResponse> Handle(CreatePetCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var species = ContractEnumMapper.ToPetSpecies(command.Species);
        var sex = ContractEnumMapper.ToPetSex(command.Sex);
        var size = ContractEnumMapper.ToPetSize(command.Size);
        var utcNow = dateTimeProvider.UtcNow;

        Pet pet = actor.Role switch
        {
            UserRole.Owner => Pet.CreateForOwner(
                actor.Id,
                command.Name,
                species,
                command.Breed,
                command.AgeText,
                sex,
                size,
                command.Sterilized,
                command.Vaccinated,
                command.Description,
                command.Temperament,
                command.MedicalHistory,
                command.LocationDistrict,
                utcNow),
            UserRole.Ong => Pet.CreateForOrganization(
                await CurrentUserContext.GetOwnedOrganizationIdAsync(dbContext, actor.Id, cancellationToken)
                    ?? throw new BusinessRuleViolationException("Ong users must create an organization before creating pets."),
                command.Name,
                species,
                command.Breed,
                command.AgeText,
                sex,
                size,
                command.Sterilized,
                command.Vaccinated,
                command.Description,
                command.Temperament,
                command.MedicalHistory,
                command.LocationDistrict,
                utcNow),
            _ => throw new ForbiddenAppException("Only users with role Owner or Ong can create pets.")
        };

        await dbContext.Pets.AddAsync(pet, cancellationToken);
        await auditService.WriteAsync(
            "Create",
            actor.Id,
            "Pet",
            pet.Id,
            before: null,
            after: pet.ToAuditSnapshot(),
            metadata: new
            {
                pet.OwnerUserId,
                pet.OrganizationId
            },
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return pet.ToDetailResponse();
    }
}
