using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Contracts.Users.Responses;
using HairyPaws.Domain.Identity.Entities;
using HairyPaws.Domain.Identity.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Identity.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role,
    string? PhoneNumber,
    string? IdentityDocument,
    string? Address);

public sealed class RegisterCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RegisterCommand, UserSummaryResponse>
{
    public async Task<UserSummaryResponse> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var normalizedIdentityDocument = NormalizeOptional(command.IdentityDocument);
        var role = ContractEnumMapper.ToUserRole(command.Role);

        if (role == UserRole.Admin)
        {
            throw new BusinessRuleViolationException("Admin users cannot be created through public registration.");
        }

        var emailExists = await dbContext.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken);
        if (emailExists)
        {
            throw new ConflictException("A user with the same email already exists.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedIdentityDocument))
        {
            var identityDocumentExists = await dbContext.Users.AnyAsync(
                user => user.IdentityDocument == normalizedIdentityDocument,
                cancellationToken);

            if (identityDocumentExists)
            {
                throw new ConflictException("A user with the same identity document already exists.");
            }
        }

        var utcNow = dateTimeProvider.UtcNow;
        var user = User.Create(
            normalizedEmail,
            string.Empty,
            command.FirstName,
            command.LastName,
            role,
            utcNow,
            command.PhoneNumber,
            normalizedIdentityDocument,
            command.Address);

        var passwordHash = passwordHasher.HashPassword(user, command.Password);
        user.ChangePassword(passwordHash, utcNow);

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return user.ToSummaryResponse();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
