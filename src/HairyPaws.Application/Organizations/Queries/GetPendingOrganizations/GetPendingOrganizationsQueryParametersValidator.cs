using FluentValidation;
using HairyPaws.Contracts.Organizations.Requests;

namespace HairyPaws.Application.Organizations.Queries.GetPendingOrganizations;

public sealed class GetPendingOrganizationsQueryParametersValidator : AbstractValidator<GetPendingOrganizationsQueryParameters>
{
    public GetPendingOrganizationsQueryParametersValidator()
    {
        RuleFor(static query => query.Page)
            .GreaterThan(0);

        RuleFor(static query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(static query => query.Search)
            .MaximumLength(200);
    }
}
