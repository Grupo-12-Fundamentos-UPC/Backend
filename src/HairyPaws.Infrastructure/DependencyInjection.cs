using HairyPaws.Application.Common.Ports;
using HairyPaws.Infrastructure.Auth;
using HairyPaws.Infrastructure.Persistence;
using HairyPaws.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HairyPaws.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<AdminSeedOptions>()
            .Bind(configuration.GetSection(AdminSeedOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));

        services.AddDbContext<ApplicationDbContext>((provider, options) =>
        {
            var connectionString = PostgresConnectionString.Resolve(provider.GetRequiredService<IConfiguration>());

            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                });
            options.UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IApplicationDbContext>(static provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ApplicationDbContextSeeder>();
        services.AddHttpContextAccessor();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }
}
