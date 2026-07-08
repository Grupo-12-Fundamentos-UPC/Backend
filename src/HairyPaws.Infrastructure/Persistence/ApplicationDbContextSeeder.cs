using HairyPaws.Application.Common.Ports;
using HairyPaws.Domain.Identity.Entities;
using HairyPaws.Domain.Identity.Enums;
using HairyPaws.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HairyPaws.Infrastructure.Persistence;

public sealed class ApplicationDbContextSeeder(
    ApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    IOptions<AdminSeedOptions> seedOptions)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var options = seedOptions.Value;
        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            return;
        }

        var normalizedEmail = options.Email.Trim().ToLowerInvariant();
        var adminExists = await dbContext.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken);
        if (adminExists)
        {
            return;
        }

        var utcNow = dateTimeProvider.UtcNow;
        var adminUser = User.Create(
            normalizedEmail,
            string.Empty,
            options.FirstName,
            options.LastName,
            UserRole.Admin,
            utcNow);

        adminUser.ChangePassword(passwordHasher.HashPassword(adminUser, options.Password), utcNow);
        adminUser.UpdateVerificationStatus(VerificationStatus.Verified, utcNow);

        await dbContext.Users.AddAsync(adminUser, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
