using FluentValidation;
using HairyPaws.Application.Common.Validation;
using HairyPaws.Contracts.Donations.Requests;
using HairyPaws.Domain.Donations.Enums;

namespace HairyPaws.Application.Donations.Queries.GetMyDonations;

public sealed class DonationsQueryParametersValidator : AbstractValidator<DonationsQueryParameters>
{
    private static readonly string[] AllowedSortFields = ["createdAt", "updatedAt", "amount", "status"];
    private static readonly string[] AllowedSortDirections = ["asc", "desc"];

    public DonationsQueryParametersValidator()
    {
        RuleFor(static query => query.Page)
            .GreaterThan(0);

        RuleFor(static query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(static query => query.Status)
            .MustBeEnumValueWhenProvided<DonationsQueryParameters, DonationStatus>();

        RuleFor(static query => query.DonationType)
            .MustBeEnumValueWhenProvided<DonationsQueryParameters, DonationType>();

        RuleFor(static query => query.SortBy)
            .Must(static value => string.IsNullOrWhiteSpace(value) || AllowedSortFields.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortFields)}.");

        RuleFor(static query => query.SortDirection)
            .Must(static value => string.IsNullOrWhiteSpace(value) || AllowedSortDirections.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortDirection must be either asc or desc.");
    }
}
