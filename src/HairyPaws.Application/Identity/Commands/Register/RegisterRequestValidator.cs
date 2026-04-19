using FluentValidation;
using HairyPaws.Application.Common.Validation;
using HairyPaws.Contracts.Identity.Requests;
using HairyPaws.Domain.Identity.Enums;

namespace HairyPaws.Application.Identity.Commands.Register;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(static request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(static request => request.Password)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(static request => request.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(static request => request.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(static request => request.Role)
            .NotEmpty()
            .MustBeEnumValue<RegisterRequest, UserRole>();

        RuleFor(static request => request.PhoneNumber)
            .MaximumLength(30);

        RuleFor(static request => request.IdentityDocument)
            .MaximumLength(50);

        RuleFor(static request => request.Address)
            .MaximumLength(500);
    }
}
