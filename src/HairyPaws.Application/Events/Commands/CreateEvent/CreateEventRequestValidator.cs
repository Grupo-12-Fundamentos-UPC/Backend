using FluentValidation;
using HairyPaws.Contracts.Events.Requests;

namespace HairyPaws.Application.Events.Commands.CreateEvent;

public sealed class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(static request => request.OrganizationId)
            .NotEmpty();

        RuleFor(static request => request.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(static request => request.Description)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(static request => request.EventDate)
            .GreaterThan(_ => DateTimeOffset.UtcNow)
            .WithMessage("EventDate must be in the future.");

        RuleFor(static request => request.Location)
            .MaximumLength(500);
    }
}
