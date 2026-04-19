using FluentValidation;
using HairyPaws.Contracts.Visits.Requests;

namespace HairyPaws.Application.Visits.Commands.RejectVisit;

public sealed class RejectVisitRequestValidator : AbstractValidator<RejectVisitRequest>
{
    public RejectVisitRequestValidator()
    {
        RuleFor(static request => request.Notes)
            .MaximumLength(1000);
    }
}
