using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Donations.Common;
using HairyPaws.Contracts.Donations.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Donations.Queries.GetDonationById;

public sealed record GetDonationByIdQuery(Guid DonationId);

public sealed class GetDonationByIdQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetDonationByIdQuery, DonationResponse>
{
    public async Task<DonationResponse> Handle(GetDonationByIdQuery query, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var donation = await dbContext.Donations
            .AsNoTracking()
            .IncludeForDetail()
            .SingleOrDefaultAsync(entity => entity.Id == query.DonationId, cancellationToken)
            ?? throw new NotFoundException("The donation was not found.");

        if (!await CurrentUserContext.CanAccessDonationAsync(dbContext, actor, donation, cancellationToken))
        {
            throw new NotFoundException("The donation was not found.");
        }

        return donation.ToResponse();
    }
}
