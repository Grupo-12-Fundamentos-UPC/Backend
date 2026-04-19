using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Users.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Users.Commands.UpdateUserStatus;

public sealed record UpdateUserStatusCommand(Guid UserId, string Status);

public sealed class UpdateUserStatusCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdateUserStatusCommand, UserSummaryResponse>
{
    public async Task<UserSummaryResponse> Handle(UpdateUserStatusCommand command, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserContext.GetRequiredUserId(currentUserService);
        var user = await dbContext.Users
            .SingleOrDefaultAsync(entity => entity.Id == command.UserId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The requested user was not found.");

        var before = user.ToAuditSnapshot();
        var previousStatus = user.Status.ToString();
        var nextStatus = ContractEnumMapper.ToUserStatus(command.Status);

        user.UpdateStatus(nextStatus, dateTimeProvider.UtcNow);

        await auditService.WriteAsync(
            "ChangeStatus",
            actorUserId,
            "User",
            user.Id,
            before,
            user.ToAuditSnapshot(),
            new
            {
                previousStatus,
                nextStatus = nextStatus.ToString()
            },
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return user.ToSummaryResponse();
    }
}
