using FluentValidation;
using HairyPaws.Contracts.Users.Requests;

namespace HairyPaws.Application.Users.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileRequestValidator : AbstractValidator<UpdateMyProfileRequest>
{
    public UpdateMyProfileRequestValidator()
    {
        RuleFor(static request => request.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(static request => request.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(static request => request.PhoneNumber)
            .MaximumLength(30);

        RuleFor(static request => request.IdentityDocument)
            .MaximumLength(50);

        RuleFor(static request => request.Address)
            .MaximumLength(500);

        RuleFor(static request => request.ProfileImagePath)
            .MaximumLength(500);
    }
}
