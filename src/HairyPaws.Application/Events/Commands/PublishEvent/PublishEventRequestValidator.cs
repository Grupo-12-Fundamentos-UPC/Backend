using FluentValidation;
using HairyPaws.Contracts.Events.Requests;

namespace HairyPaws.Application.Events.Commands.PublishEvent;

public sealed class PublishEventRequestValidator : AbstractValidator<PublishEventRequest>
{
}
