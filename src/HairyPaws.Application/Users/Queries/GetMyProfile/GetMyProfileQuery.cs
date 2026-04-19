using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Contracts.Users.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Users.Queries.GetMyProfile;

public sealed record GetMyProfileQuery;

public sealed class GetMyProfileQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetMyProfileQuery, UserProfileResponse>
{
    public async Task<UserProfileResponse> Handle(GetMyProfileQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAppException("Authentication is required.");

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == userId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The current user was not found.");

        return user.ToProfileResponse();
    }
}
