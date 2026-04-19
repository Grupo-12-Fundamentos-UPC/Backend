using FluentValidation;
using HairyPaws.Application.Common.Validation;
using HairyPaws.Contracts.Events.Requests;
using HairyPaws.Domain.Events.Enums;

namespace HairyPaws.Application.Events.Queries.GetMyEvents;

public sealed class MyEventsQueryParametersValidator : AbstractValidator<MyEventsQueryParameters>
{
    private static readonly string[] AllowedSortFields = ["eventDate", "createdAt", "status", "title"];
    private static readonly string[] AllowedSortDirections = ["asc", "desc"];

    public MyEventsQueryParametersValidator()
    {
        RuleFor(static query => query.Page)
            .GreaterThan(0);

        RuleFor(static query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(static query => query.Status)
            .MustBeEnumValueWhenProvided<MyEventsQueryParameters, EventStatus>();

        RuleFor(static query => query.Search)
            .MaximumLength(200);

        RuleFor(static query => query)
            .Must(static query => !query.FromDate.HasValue || !query.ToDate.HasValue || query.FromDate <= query.ToDate)
            .WithMessage("FromDate must be less than or equal to ToDate.");

        RuleFor(static query => query.SortBy)
            .Must(static value => string.IsNullOrWhiteSpace(value) || AllowedSortFields.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortFields)}.");

        RuleFor(static query => query.SortDirection)
            .Must(static value => string.IsNullOrWhiteSpace(value) || AllowedSortDirections.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortDirection must be either asc or desc.");
    }
}
