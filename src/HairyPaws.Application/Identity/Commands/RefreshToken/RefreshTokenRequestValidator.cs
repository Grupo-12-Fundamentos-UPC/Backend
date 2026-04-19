using FluentValidation;
using HairyPaws.Contracts.Identity.Requests;

namespace HairyPaws.Application.Identity.Commands.RefreshToken;

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(static request => request.RefreshToken)
            .NotEmpty();
    }
}
