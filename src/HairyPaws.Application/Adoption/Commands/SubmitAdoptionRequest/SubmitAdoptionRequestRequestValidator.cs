using FluentValidation;
using HairyPaws.Contracts.Adoption.Requests;

namespace HairyPaws.Application.Adoption.Commands.SubmitAdoptionRequest;

public sealed class SubmitAdoptionRequestRequestValidator : AbstractValidator<SubmitAdoptionRequestRequest>
{
    public SubmitAdoptionRequestRequestValidator()
    {
        RuleFor(static request => request.PetId)
            .NotEmpty();

        RuleFor(static request => request.ContactPhone)
            .NotEmpty()
            .MaximumLength(30);

        RuleFor(static request => request.LivingConditions)
            .MaximumLength(2000);

        RuleFor(static request => request.WhyAdopt)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
