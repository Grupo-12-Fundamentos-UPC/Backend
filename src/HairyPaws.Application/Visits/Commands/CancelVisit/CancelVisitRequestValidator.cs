using FluentValidation;
using HairyPaws.Contracts.Visits.Requests;

namespace HairyPaws.Application.Visits.Commands.CancelVisit;

public sealed class CancelVisitRequestValidator : AbstractValidator<CancelVisitRequest>
{
    public CancelVisitRequestValidator()
    {
        RuleFor(static request => request.Notes)
            .MaximumLength(1000);
    }
}
