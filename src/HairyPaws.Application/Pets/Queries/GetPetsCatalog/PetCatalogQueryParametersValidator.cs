using FluentValidation;
using HairyPaws.Application.Common.Validation;
using HairyPaws.Contracts.Pets.Requests;
using HairyPaws.Domain.Pets.Enums;

namespace HairyPaws.Application.Pets.Queries.GetPetsCatalog;

public sealed class PetCatalogQueryParametersValidator : AbstractValidator<PetCatalogQueryParameters>
{
    private static readonly string[] AllowedSortFields = ["createdAt", "publishedAt", "name"];
    private static readonly string[] AllowedSortDirections = ["asc", "desc"];

    public PetCatalogQueryParametersValidator()
    {
        RuleFor(static query => query.Page)
            .GreaterThan(0);

        RuleFor(static query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(static query => query.Species)
            .MustBeEnumValueWhenProvided<PetCatalogQueryParameters, PetSpecies>();

        RuleFor(static query => query.Sex)
            .MustBeEnumValueWhenProvided<PetCatalogQueryParameters, PetSex>();

        RuleFor(static query => query.Size)
            .MustBeEnumValueWhenProvided<PetCatalogQueryParameters, PetSize>();

        RuleFor(static query => query.LocationDistrict)
            .MaximumLength(150);

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
