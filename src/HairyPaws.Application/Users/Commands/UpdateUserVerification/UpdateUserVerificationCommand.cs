using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Users.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Users.Commands.UpdateUserVerification;

public sealed record UpdateUserVerificationCommand(Guid UserId, string VerificationStatus);

public sealed class UpdateUserVerificationCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdateUserVerificationCommand, UserSummaryResponse>
{
    public async Task<UserSummaryResponse> Handle(UpdateUserVerificationCommand command, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserContext.GetRequiredUserId(currentUserService);
        var user = await dbContext.Users
            .SingleOrDefaultAsync(entity => entity.Id == command.UserId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The requested user was not found.");

        var before = user.ToAuditSnapshot();
        var previousVerificationStatus = user.VerificationStatus.ToString();
        var nextVerificationStatus = ContractEnumMapper.ToVerificationStatus(command.VerificationStatus);

        user.UpdateVerificationStatus(
            nextVerificationStatus,
            dateTimeProvider.UtcNow);

        await auditService.WriteAsync(
            "ChangeVerificationStatus",
            actorUserId,
            "User",
            user.Id,
            before,
            user.ToAuditSnapshot(),
            new
            {
                previousVerificationStatus,
                nextVerificationStatus = nextVerificationStatus.ToString()
            },
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return user.ToSummaryResponse();
    }
}
