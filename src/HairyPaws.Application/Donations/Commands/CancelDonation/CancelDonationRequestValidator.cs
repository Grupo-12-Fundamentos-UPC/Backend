using FluentValidation;
using HairyPaws.Contracts.Donations.Requests;

namespace HairyPaws.Application.Donations.Commands.CancelDonation;

public sealed class CancelDonationRequestValidator : AbstractValidator<CancelDonationRequest>
{
}
