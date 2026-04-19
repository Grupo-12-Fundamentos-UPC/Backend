using FluentValidation;
using HairyPaws.Contracts.Visits.Requests;

namespace HairyPaws.Application.Visits.Commands.ApproveVisit;

public sealed class ApproveVisitRequestValidator : AbstractValidator<ApproveVisitRequest>
{
    public ApproveVisitRequestValidator()
    {
        RuleFor(static request => request.Notes)
            .MaximumLength(1000);
    }
}
