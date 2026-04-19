using FluentValidation;
using HairyPaws.Contracts.Identity.Requests;

namespace HairyPaws.Application.Identity.Commands.AdminResetPassword;

public sealed class AdminResetPasswordRequestValidator : AbstractValidator<AdminResetPasswordRequest>
{
    public AdminResetPasswordRequestValidator()
    {
        RuleFor(static request => request.UserId)
            .NotEmpty();

        RuleFor(static request => request.NewPassword)
            .NotEmpty()
            .MinimumLength(8);
    }
}
