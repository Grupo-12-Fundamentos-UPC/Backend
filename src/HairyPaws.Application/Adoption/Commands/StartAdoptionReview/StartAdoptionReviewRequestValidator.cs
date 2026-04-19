using FluentValidation;
using HairyPaws.Contracts.Adoption.Requests;

namespace HairyPaws.Application.Adoption.Commands.StartAdoptionReview;

public sealed class StartAdoptionReviewRequestValidator : AbstractValidator<StartAdoptionReviewRequest>
{
    public StartAdoptionReviewRequestValidator()
    {
        RuleFor(static request => request.Notes)
            .MaximumLength(2000);
    }
}
