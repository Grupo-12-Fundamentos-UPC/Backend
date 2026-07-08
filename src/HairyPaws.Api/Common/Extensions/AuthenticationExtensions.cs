using System.Text;
using HairyPaws.Application.Common.Security;
using HairyPaws.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

namespace HairyPaws.Api.Common.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerOptionsSetup>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.RequireAuthenticatedUser,
                policy => policy.RequireAuthenticatedUser());

            options.AddPolicy(
                AuthorizationPolicies.RequireAdmin,
                policy => policy.RequireAuthenticatedUser().RequireRole("Admin"));

            options.AddPolicy(
                AuthorizationPolicies.RequireAdopter,
                policy => policy.RequireAuthenticatedUser().RequireRole("Adopter"));

            options.AddPolicy(
                AuthorizationPolicies.RequireOng,
                policy => policy.RequireAuthenticatedUser().RequireRole("Ong"));

            options.AddPolicy(
                AuthorizationPolicies.RequireOwnerOrOng,
                policy => policy.RequireAuthenticatedUser().RequireRole("Owner", "Ong"));
        });

        return services;
    }
}

internal sealed class JwtBearerOptionsSetup(IOptions<JwtOptions> jwtOptions)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (!string.Equals(name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return;
        }

        var settings = jwtOptions.Value;
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret))
        };
    }

    public void Configure(JwtBearerOptions options) => Configure(Microsoft.Extensions.Options.Options.DefaultName, options);
}
