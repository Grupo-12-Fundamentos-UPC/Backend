using FluentValidation;
using HairyPaws.Application.Common.Validation;
using HairyPaws.Contracts.Users.Requests;
using HairyPaws.Domain.Identity.Enums;

namespace HairyPaws.Application.Users.Commands.UpdateUserStatus;

public sealed class UpdateUserStatusRequestValidator : AbstractValidator<UpdateUserStatusRequest>
{
    public UpdateUserStatusRequestValidator()
    {
        RuleFor(static request => request.Status)
            .NotEmpty()
            .MustBeEnumValue<UpdateUserStatusRequest, UserStatus>();
    }
}
