using FluentValidation;
using HairyPaws.Contracts.Organizations.Requests;

namespace HairyPaws.Application.Organizations.Commands.RejectOrganization;

public sealed class RejectOrganizationRequestValidator : AbstractValidator<RejectOrganizationRequest>
{
    public RejectOrganizationRequestValidator()
    {
        RuleFor(static request => request.Notes)
            .MaximumLength(1000);
    }
}
