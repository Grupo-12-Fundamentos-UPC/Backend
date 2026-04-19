using FluentValidation;
using HairyPaws.Contracts.Visits.Requests;

namespace HairyPaws.Application.Visits.Commands.CreateVisit;

public sealed class CreateVisitRequestValidator : AbstractValidator<CreateVisitRequest>
{
    public CreateVisitRequestValidator()
    {
        RuleFor(static request => request.ScheduledAt)
            .Must(static scheduledAt => scheduledAt > DateTimeOffset.UtcNow)
            .WithMessage("ScheduledAt must be a future date and time.");

        RuleFor(static request => request.Location)
            .MaximumLength(500);

        RuleFor(static request => request.Notes)
            .MaximumLength(1000);
    }
}
