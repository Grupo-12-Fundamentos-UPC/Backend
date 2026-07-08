using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Contracts.Users.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Identity.Queries.GetCurrentAuthUser;

public sealed record GetCurrentAuthUserQuery;

public sealed class GetCurrentAuthUserQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetCurrentAuthUserQuery, UserSummaryResponse>
{
    public async Task<UserSummaryResponse> Handle(GetCurrentAuthUserQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAppException("Authentication is required.");

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == userId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The current user was not found.");

        return user.ToSummaryResponse();
    }
}
