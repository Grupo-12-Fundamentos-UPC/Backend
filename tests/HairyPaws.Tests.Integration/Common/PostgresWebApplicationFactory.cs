using System.Net.Http.Headers;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Infrastructure.Auth;
using HairyPaws.Infrastructure.Persistence;
using HairyPaws.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace HairyPaws.Tests.Integration.Common;

public sealed class PostgresWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _storageRoot = Path.Combine(
        Path.GetTempPath(),
        "hairypaws-integration-tests",
        Guid.NewGuid().ToString("N"));

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("hairy_paws_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private NpgsqlConnection? _connection;
    private Respawner? _respawner;

    public string AdminEmail => "admin@hairypaws.test";

    public string AdminPassword => "Admin123!";

    public HttpClient CreateApiClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _database.GetConnectionString(),
                ["Jwt:Issuer"] = "HairyPaws.Tests",
                ["Jwt:Audience"] = "HairyPaws.Tests.Clients",
                ["Jwt:Secret"] = "ThisIsATestJwtSecretThatIsLongEnough123456789",
                ["Jwt:AccessTokenLifetimeMinutes"] = "30",
                ["Jwt:RefreshTokenLifetimeDays"] = "14",
                ["Seed:AdminUser:Email"] = AdminEmail,
                ["Seed:AdminUser:Password"] = AdminPassword,
                ["Seed:AdminUser:FirstName"] = "System",
                ["Seed:AdminUser:LastName"] = "Administrator",
                ["Storage:UploadsPath"] = Path.Combine(_storageRoot, "uploads")
            };

            configurationBuilder.AddInMemoryCollection(settings);
        });
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        await MigrateDatabaseAsync();
        using var _ = CreateApiClient();

        _connection = new NpgsqlConnection(_database.GetConnectionString());
        await _connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore = [new Table("__EFMigrationsHistory")]
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await _database.DisposeAsync();
        DeleteStorageRoot();
    }

    public async Task ResetDatabaseAsync()
    {
        if (_connection is null || _respawner is null)
        {
            throw new InvalidOperationException("The integration test database has not been initialized.");
        }

        await _respawner.ResetAsync(_connection);
        await SeedAdminUserAsync();
        ResetUploadsDirectory();
    }

    public static void SetBearerToken(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task ExecuteDbContextAsync(Func<ApplicationDbContext, Task> action)
    {
        await using var dbContext = CreateDbContext();
        await action(dbContext);
        await dbContext.SaveChangesAsync();
    }

    public async Task<T> ExecuteDbContextAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        await using var dbContext = CreateDbContext();
        var result = await action(dbContext);
        await dbContext.SaveChangesAsync();
        return result;
    }

    private async Task SeedAdminUserAsync()
    {
        await using var dbContext = CreateDbContext();
        IPasswordHasher passwordHasher = new PasswordHasherService();
        IDateTimeProvider dateTimeProvider = new SystemDateTimeProvider();
        var options = Options.Create(new AdminSeedOptions
        {
            Email = AdminEmail,
            Password = AdminPassword,
            FirstName = "System",
            LastName = "Administrator"
        });

        var seeder = new ApplicationDbContextSeeder(dbContext, passwordHasher, dateTimeProvider, options);
        await seeder.SeedAsync(CancellationToken.None);
    }

    private async Task MigrateDatabaseAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    private ApplicationDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(_database.GetConnectionString());
        optionsBuilder.UseSnakeCaseNamingConvention();

        return new ApplicationDbContext(optionsBuilder.Options, new SystemDateTimeProvider());
    }

    private void ResetUploadsDirectory()
    {
        using var scope = Services.CreateScope();
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var storageOptions = scope.ServiceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
        var uploadsPath = storageOptions.GetUploadsRoot(environment.ContentRootPath);
        Directory.CreateDirectory(uploadsPath);

        var rootDirectory = new DirectoryInfo(uploadsPath);
        foreach (var file in rootDirectory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            DeleteFileWithRetry(file);
        }
        
        foreach (var directory in rootDirectory.EnumerateDirectories("*", SearchOption.AllDirectories).OrderByDescending(static directory => directory.FullName.Length))
        {
            DeleteDirectoryIfEmpty(directory);
        }
    }

    private static void DeleteFileWithRetry(FileInfo file)
    {
        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (!file.Exists)
                {
                    return;
                }

                file.IsReadOnly = false;
                file.Delete();
                return;
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }
            catch (FileNotFoundException)
            {
                return;
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                Thread.Sleep(100);
                file.Refresh();
            }
            catch (UnauthorizedAccessException) when (attempt < maxAttempts)
            {
                Thread.Sleep(100);
                file.Refresh();
            }
        }
    }

    private static void DeleteDirectoryIfEmpty(DirectoryInfo directory)
    {
        try
        {
            if (!directory.Exists)
            {
                return;
            }

            directory.Delete(recursive: false);
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void DeleteStorageRoot()
    {
        try
        {
            if (Directory.Exists(_storageRoot))
            {
                Directory.Delete(_storageRoot, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
