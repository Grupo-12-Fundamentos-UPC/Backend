using FluentValidation;
using HairyPaws.Application.Common.Validation;
using HairyPaws.Contracts.Users.Requests;
using HairyPaws.Domain.Identity.Enums;

namespace HairyPaws.Application.Users.Commands.UpdateUserVerification;

public sealed class UpdateUserVerificationRequestValidator : AbstractValidator<UpdateUserVerificationRequest>
{
    public UpdateUserVerificationRequestValidator()
    {
        RuleFor(static request => request.VerificationStatus)
            .NotEmpty()
            .MustBeEnumValue<UpdateUserVerificationRequest, VerificationStatus>();
    }
}
