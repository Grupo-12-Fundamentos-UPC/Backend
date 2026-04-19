using FluentValidation;
using HairyPaws.Application.Common.Validation;
using HairyPaws.Contracts.Donations.Requests;
using HairyPaws.Domain.Donations.Enums;

namespace HairyPaws.Application.Donations.Queries.GetOrganizationDonations;

public sealed class OrganizationDonationsQueryParametersValidator : AbstractValidator<OrganizationDonationsQueryParameters>
{
    private static readonly string[] AllowedSortFields = ["createdAt", "updatedAt", "amount", "status", "donor"];
    private static readonly string[] AllowedSortDirections = ["asc", "desc"];

    public OrganizationDonationsQueryParametersValidator()
    {
        RuleFor(static query => query.Page)
            .GreaterThan(0);

        RuleFor(static query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(static query => query.Status)
            .MustBeEnumValueWhenProvided<OrganizationDonationsQueryParameters, DonationStatus>();

        RuleFor(static query => query.DonationType)
            .MustBeEnumValueWhenProvided<OrganizationDonationsQueryParameters, DonationType>();

        RuleFor(static query => query.Search)
            .MaximumLength(200);

        RuleFor(static query => query.SortBy)
            .Must(static value => string.IsNullOrWhiteSpace(value) || AllowedSortFields.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortFields)}.");

        RuleFor(static query => query.SortDirection)
            .Must(static value => string.IsNullOrWhiteSpace(value) || AllowedSortDirections.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortDirection must be either asc or desc.");
    }
}
