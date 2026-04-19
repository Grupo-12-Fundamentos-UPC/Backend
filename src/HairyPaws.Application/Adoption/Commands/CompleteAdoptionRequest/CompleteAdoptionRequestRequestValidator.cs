using FluentValidation;
using HairyPaws.Contracts.Adoption.Requests;

namespace HairyPaws.Application.Adoption.Commands.CompleteAdoptionRequest;

public sealed class CompleteAdoptionRequestRequestValidator : AbstractValidator<CompleteAdoptionRequestRequest>
{
    public CompleteAdoptionRequestRequestValidator()
    {
        RuleFor(static request => request.Notes)
            .MaximumLength(2000);
    }
}
