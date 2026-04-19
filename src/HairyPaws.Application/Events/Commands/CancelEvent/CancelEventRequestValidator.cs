using FluentValidation;
using HairyPaws.Contracts.Events.Requests;

namespace HairyPaws.Application.Events.Commands.CancelEvent;

public sealed class CancelEventRequestValidator : AbstractValidator<CancelEventRequest>
{
}
