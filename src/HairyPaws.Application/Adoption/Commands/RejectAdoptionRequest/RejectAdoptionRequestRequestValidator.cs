using FluentValidation;
using HairyPaws.Contracts.Adoption.Requests;

namespace HairyPaws.Application.Adoption.Commands.RejectAdoptionRequest;

public sealed class RejectAdoptionRequestRequestValidator : AbstractValidator<RejectAdoptionRequestRequest>
{
    public RejectAdoptionRequestRequestValidator()
    {
        RuleFor(static request => request.Notes)
            .MaximumLength(2000);
    }
}
