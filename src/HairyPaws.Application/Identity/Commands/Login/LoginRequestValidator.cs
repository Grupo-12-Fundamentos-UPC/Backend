using FluentValidation;
using HairyPaws.Contracts.Identity.Requests;

namespace HairyPaws.Application.Identity.Commands.Login;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(static request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(static request => request.Password)
            .NotEmpty();
    }
}
