using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HairyPaws.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        this IServiceProvider serviceProvider,
        bool runMigrations = true,
        bool seedAdmin = true,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (runMigrations)
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        if (seedAdmin)
        {
            var seeder = scope.ServiceProvider.GetRequiredService<ApplicationDbContextSeeder>();
            await seeder.SeedAsync(cancellationToken);
        }
    }
}
