using FluentValidation;
using HairyPaws.Contracts.Events.Requests;

namespace HairyPaws.Application.Events.Commands.UpdateEvent;

public sealed class UpdateEventRequestValidator : AbstractValidator<UpdateEventRequest>
{
    public UpdateEventRequestValidator()
    {
        RuleFor(static request => request)
            .Must(HasAtLeastOneValue)
            .WithMessage("At least one field must be provided.");

        RuleFor(static request => request.Title)
            .NotEmpty()
            .MaximumLength(200)
            .When(static request => request.Title is not null);

        RuleFor(static request => request.Description)
            .NotEmpty()
            .MaximumLength(4000)
            .When(static request => request.Description is not null);

        RuleFor(static request => request.EventDate)
            .GreaterThan(_ => DateTimeOffset.UtcNow)
            .WithMessage("EventDate must be in the future.")
            .When(static request => request.EventDate.HasValue);

        RuleFor(static request => request.Location)
            .MaximumLength(500);
    }

    private static bool HasAtLeastOneValue(UpdateEventRequest request)
    {
        return request.Title is not null ||
               request.Description is not null ||
               request.EventDate.HasValue ||
               request.Location is not null ||
               request.IsVolunteerEvent.HasValue;
    }
}
