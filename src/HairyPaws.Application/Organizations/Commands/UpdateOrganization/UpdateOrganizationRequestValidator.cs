using FluentValidation;
using HairyPaws.Contracts.Organizations.Requests;

namespace HairyPaws.Application.Organizations.Commands.UpdateOrganization;

public sealed class UpdateOrganizationRequestValidator : AbstractValidator<UpdateOrganizationRequest>
{
    public UpdateOrganizationRequestValidator()
    {
        RuleFor(static request => request)
            .Must(HasAtLeastOneValue)
            .WithMessage("At least one field must be provided.");

        RuleFor(static request => request.Name)
            .NotEmpty()
            .MaximumLength(200)
            .When(static request => request.Name is not null);

        RuleFor(static request => request.Ruc)
            .NotEmpty()
            .Matches("^[0-9]{11}$")
            .WithMessage("Ruc must contain exactly 11 digits.")
            .When(static request => request.Ruc is not null);

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

    private static bool HasAtLeastOneValue(UpdateOrganizationRequest request)
    {
        return request.Name is not null ||
               request.Ruc is not null ||
               request.Description is not null ||
               request.Address is not null ||
               request.Phone is not null ||
               request.Email is not null;
    }
}
