using FluentValidation;
using HairyPaws.Contracts.Organizations.Requests;

namespace HairyPaws.Application.Organizations.Commands.VerifyOrganization;

public sealed class VerifyOrganizationRequestValidator : AbstractValidator<VerifyOrganizationRequest>
{
    public VerifyOrganizationRequestValidator()
    {
        RuleFor(static request => request.Notes)
            .MaximumLength(1000);
    }
}
