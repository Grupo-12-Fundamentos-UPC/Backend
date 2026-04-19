using FluentValidation;
using HairyPaws.Application.Common.Validation;
using HairyPaws.Contracts.Adoption.Requests;
using HairyPaws.Domain.Adoption.Enums;

namespace HairyPaws.Application.Adoption.Queries.GetPetAdoptionRequests;

public sealed class PetAdoptionRequestsQueryParametersValidator : AbstractValidator<PetAdoptionRequestsQueryParameters>
{
    private static readonly string[] AllowedSortFields = ["createdAt", "updatedAt", "status", "adopter"];
    private static readonly string[] AllowedSortDirections = ["asc", "desc"];

    public PetAdoptionRequestsQueryParametersValidator()
    {
        RuleFor(static query => query.Page)
            .GreaterThan(0);

        RuleFor(static query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(static query => query.Status)
            .MustBeEnumValueWhenProvided<PetAdoptionRequestsQueryParameters, AdoptionRequestStatus>();

        RuleFor(static query => query.Search)
            .MaximumLength(200);

        RuleFor(static query => query.SortBy)
            .Must(static sortBy => string.IsNullOrWhiteSpace(sortBy) || AllowedSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortFields)}.");

        RuleFor(static query => query.SortDirection)
            .Must(static direction => string.IsNullOrWhiteSpace(direction) || AllowedSortDirections.Contains(direction, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortDirection must be either asc or desc.");
    }
}
