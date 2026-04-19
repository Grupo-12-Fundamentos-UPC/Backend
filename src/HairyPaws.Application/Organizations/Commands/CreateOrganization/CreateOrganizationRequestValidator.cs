using FluentValidation;
using HairyPaws.Contracts.Organizations.Requests;

namespace HairyPaws.Application.Organizations.Commands.CreateOrganization;

public sealed class CreateOrganizationRequestValidator : AbstractValidator<CreateOrganizationRequest>
{
    public CreateOrganizationRequestValidator()
    {
        RuleFor(static request => request.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(static request => request.Ruc)
            .NotEmpty()
            .Matches("^[0-9]{11}$")
            .WithMessage("Ruc must contain exactly 11 digits.");

        RuleFor(static request => request.Description)
            .MaximumLength(2000);

        RuleFor(static request => request.Address)
            .MaximumLength(500);

        RuleFor(static request => request.Phone)
            .MaximumLength(30);

        RuleFor(static request => request.Email)
            .MaximumLength(320)
            .EmailAddress()
            .When(static request => !string.IsNullOrWhiteSpace(request.Email));
    }
}
