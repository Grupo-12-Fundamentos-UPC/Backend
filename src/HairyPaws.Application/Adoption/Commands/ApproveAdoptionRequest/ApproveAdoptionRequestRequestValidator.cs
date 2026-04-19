using FluentValidation;
using HairyPaws.Contracts.Adoption.Requests;

namespace HairyPaws.Application.Adoption.Commands.ApproveAdoptionRequest;

public sealed class ApproveAdoptionRequestRequestValidator : AbstractValidator<ApproveAdoptionRequestRequest>
{
    public ApproveAdoptionRequestRequestValidator()
    {
        RuleFor(static request => request.Notes)
            .MaximumLength(2000);
    }
}
