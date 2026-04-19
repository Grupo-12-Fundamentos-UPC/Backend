using FluentValidation;
using HairyPaws.Contracts.Adoption.Requests;

namespace HairyPaws.Application.Adoption.Commands.CancelAdoptionRequest;

public sealed class CancelAdoptionRequestRequestValidator : AbstractValidator<CancelAdoptionRequestRequest>
{
    public CancelAdoptionRequestRequestValidator()
    {
        RuleFor(static request => request.Notes)
            .MaximumLength(2000);
    }
}
