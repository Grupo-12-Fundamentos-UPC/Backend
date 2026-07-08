using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Contracts.Users.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Users.Commands.UpdateMyProfile;

public sealed record UpdateMyProfileCommand(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? IdentityDocument,
    string? Address,
    string? ProfileImagePath);

public sealed class UpdateMyProfileCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateMyProfileCommand, UserProfileResponse>
{
    public async Task<UserProfileResponse> Handle(UpdateMyProfileCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAppException("Authentication is required.");
        var normalizedIdentityDocument = NormalizeOptional(command.IdentityDocument);

        var user = await dbContext.Users
            .SingleOrDefaultAsync(entity => entity.Id == userId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The current user was not found.");

        if (!string.IsNullOrWhiteSpace(normalizedIdentityDocument))
        {
            var duplicateIdentityDocument = await dbContext.Users.AnyAsync(
                entity => entity.Id != userId && entity.DeletedAt == null && entity.IdentityDocument == normalizedIdentityDocument,
                cancellationToken);

            if (duplicateIdentityDocument)
            {
                throw new ConflictException("A user with the same identity document already exists.");
            }
        }

        user.UpdateProfile(
            command.FirstName,
            command.LastName,
            command.PhoneNumber,
            normalizedIdentityDocument,
            command.Address,
            command.ProfileImagePath,
            dateTimeProvider.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return user.ToProfileResponse();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
