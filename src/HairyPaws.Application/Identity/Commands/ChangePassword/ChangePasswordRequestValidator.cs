using FluentValidation;
using HairyPaws.Contracts.Identity.Requests;

namespace HairyPaws.Application.Identity.Commands.ChangePassword;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(static request => request.CurrentPassword)
            .NotEmpty();

        RuleFor(static request => request.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .NotEqual(static request => request.CurrentPassword);
    }
}
