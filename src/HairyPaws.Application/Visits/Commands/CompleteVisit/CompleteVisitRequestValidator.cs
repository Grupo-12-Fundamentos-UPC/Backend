using FluentValidation;
using HairyPaws.Contracts.Visits.Requests;

namespace HairyPaws.Application.Visits.Commands.CompleteVisit;

public sealed class CompleteVisitRequestValidator : AbstractValidator<CompleteVisitRequest>
{
    public CompleteVisitRequestValidator()
    {
        RuleFor(static request => request.Notes)
            .MaximumLength(1000);
    }
}
