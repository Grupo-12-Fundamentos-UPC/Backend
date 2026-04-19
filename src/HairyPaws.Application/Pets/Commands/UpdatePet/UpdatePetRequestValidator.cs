using FluentValidation;
using HairyPaws.Application.Common.Validation;
using HairyPaws.Contracts.Pets.Requests;
using HairyPaws.Domain.Pets.Enums;

namespace HairyPaws.Application.Pets.Commands.UpdatePet;

public sealed class UpdatePetRequestValidator : AbstractValidator<UpdatePetRequest>
{
    public UpdatePetRequestValidator()
    {
        RuleFor(static request => request)
            .Must(HasAtLeastOneValue)
            .WithMessage("At least one field must be provided.");

        RuleFor(static request => request.Name)
            .MaximumLength(150);

        RuleFor(static request => request.Species)
            .MustBeEnumValueWhenProvided<UpdatePetRequest, PetSpecies>();

        RuleFor(static request => request.Breed)
            .MaximumLength(150);

        RuleFor(static request => request.AgeText)
            .MaximumLength(100);

        RuleFor(static request => request.Sex)
            .MustBeEnumValueWhenProvided<UpdatePetRequest, PetSex>();

        RuleFor(static request => request.Size)
            .MustBeEnumValueWhenProvided<UpdatePetRequest, PetSize>();

        RuleFor(static request => request.Description)
            .MaximumLength(4000);

        RuleFor(static request => request.Temperament)
            .MaximumLength(1000);

        RuleFor(static request => request.MedicalHistory)
            .MaximumLength(2000);

        RuleFor(static request => request.LocationDistrict)
            .MaximumLength(150);
    }

    private static bool HasAtLeastOneValue(UpdatePetRequest request)
    {
        return request.Name is not null ||
               request.Species is not null ||
               request.Breed is not null ||
               request.AgeText is not null ||
               request.Sex is not null ||
               request.Size is not null ||
               request.Sterilized.HasValue ||
               request.Vaccinated.HasValue ||
               request.Description is not null ||
               request.Temperament is not null ||
               request.MedicalHistory is not null ||
               request.LocationDistrict is not null;
    }
}
