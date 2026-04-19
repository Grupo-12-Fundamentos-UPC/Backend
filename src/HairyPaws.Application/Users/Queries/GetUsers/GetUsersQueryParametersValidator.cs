using FluentValidation;
using HairyPaws.Application.Common.Validation;
using HairyPaws.Contracts.Common.Requests;
using HairyPaws.Domain.Identity.Enums;

namespace HairyPaws.Application.Users.Queries.GetUsers;

public sealed class GetUsersQueryParametersValidator : AbstractValidator<GetUsersQueryParameters>
{
    public GetUsersQueryParametersValidator()
    {
        RuleFor(static query => query.PageNumber)
            .GreaterThan(0);

        RuleFor(static query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(static query => query.Role)
            .MustBeEnumValueWhenProvided<GetUsersQueryParameters, UserRole>();

        RuleFor(static query => query.Status)
            .MustBeEnumValueWhenProvided<GetUsersQueryParameters, UserStatus>();

        RuleFor(static query => query.VerificationStatus)
            .MustBeEnumValueWhenProvided<GetUsersQueryParameters, VerificationStatus>();

        RuleFor(static query => query.Search)
            .MaximumLength(200);
    }
}
