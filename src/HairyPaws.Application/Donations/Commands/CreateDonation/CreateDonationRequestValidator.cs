using FluentValidation;
using HairyPaws.Application.Common.Validation;
using HairyPaws.Contracts.Donations.Requests;
using HairyPaws.Domain.Donations.Enums;

namespace HairyPaws.Application.Donations.Commands.CreateDonation;

public sealed class CreateDonationItemRequestValidator : AbstractValidator<CreateDonationItemRequest>
{
    public CreateDonationItemRequestValidator()
    {
        RuleFor(static request => request.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(static request => request.Quantity)
            .GreaterThan(0);

        RuleFor(static request => request.Description)
            .MaximumLength(1000);
    }
}

public sealed class CreateDonationRequestValidator : AbstractValidator<CreateDonationRequest>
{
    public CreateDonationRequestValidator()
    {
        RuleFor(static request => request.OrganizationId)
            .NotEmpty();

        RuleFor(static request => request.DonationType)
            .NotEmpty()
            .MustBeEnumValue<CreateDonationRequest, DonationType>();

        RuleFor(static request => request.Amount)
            .NotNull()
            .GreaterThan(0)
            .When(static request => IsType(request.DonationType, DonationType.Money));

        RuleFor(static request => request.Items)
            .Must(static items => items is { Count: > 0 })
            .WithMessage("At least one item is required for item donations.")
            .When(static request => IsType(request.DonationType, DonationType.Items));

        RuleForEach(static request => request.Items)
            .SetValidator(new CreateDonationItemRequestValidator());

        RuleFor(static request => request.TransactionId)
            .MaximumLength(100);

        RuleFor(static request => request.Notes)
            .MaximumLength(2000);
    }

    private static bool IsType(string? value, DonationType donationType)
        => Enum.TryParse<DonationType>(value, true, out var parsedValue) && parsedValue == donationType;
}
