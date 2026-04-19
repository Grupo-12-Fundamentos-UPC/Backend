using FluentValidation;
using HairyPaws.Application.Common.Validation;
using HairyPaws.Contracts.Pets.Requests;
using HairyPaws.Domain.Pets.Enums;

namespace HairyPaws.Application.Pets.Commands.CreatePet;

public sealed class CreatePetRequestValidator : AbstractValidator<CreatePetRequest>
{
    public CreatePetRequestValidator()
    {
        RuleFor(static request => request.Name)
            .MaximumLength(150);

        RuleFor(static request => request.Species)
            .NotEmpty()
            .MustBeEnumValue<CreatePetRequest, PetSpecies>();

        RuleFor(static request => request.Breed)
            .MaximumLength(150);

        RuleFor(static request => request.AgeText)
            .MaximumLength(100);

        RuleFor(static request => request.Sex)
            .NotEmpty()
            .MustBeEnumValue<CreatePetRequest, PetSex>();

        RuleFor(static request => request.Size)
            .NotEmpty()
            .MustBeEnumValue<CreatePetRequest, PetSize>();

        RuleFor(static request => request.Description)
            .MaximumLength(4000);

        RuleFor(static request => request.Temperament)
            .MaximumLength(1000);

        RuleFor(static request => request.MedicalHistory)
            .MaximumLength(2000);

        RuleFor(static request => request.LocationDistrict)
            .MaximumLength(150);
    }
}
