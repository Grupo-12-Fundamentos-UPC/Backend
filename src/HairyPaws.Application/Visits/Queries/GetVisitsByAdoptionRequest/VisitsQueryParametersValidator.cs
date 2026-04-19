using FluentValidation;
using HairyPaws.Application.Common.Validation;
using HairyPaws.Contracts.Visits.Requests;
using HairyPaws.Domain.Visits.Enums;

namespace HairyPaws.Application.Visits.Queries.GetVisitsByAdoptionRequest;

public sealed class VisitsQueryParametersValidator : AbstractValidator<VisitsQueryParameters>
{
    private static readonly string[] AllowedSortFields = ["scheduledAt", "createdAt", "status"];
    private static readonly string[] AllowedSortDirections = ["asc", "desc"];

    public VisitsQueryParametersValidator()
    {
        RuleFor(static query => query.Page)
            .GreaterThan(0);

        RuleFor(static query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(static query => query.Status)
            .MustBeEnumValueWhenProvided<VisitsQueryParameters, VisitStatus>();

        RuleFor(static query => query.SortBy)
            .Must(static sortBy => string.IsNullOrWhiteSpace(sortBy) || AllowedSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortFields)}.");

        RuleFor(static query => query.SortDirection)
            .Must(static direction => string.IsNullOrWhiteSpace(direction) || AllowedSortDirections.Contains(direction, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortDirection must be either asc or desc.");
    }
}
