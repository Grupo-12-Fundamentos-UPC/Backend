using Microsoft.Extensions.Configuration;
using Npgsql;

namespace HairyPaws.Infrastructure.Persistence;

internal static class PostgresConnectionString
{
    public static string Resolve(IConfiguration configuration)
    {
        var databaseUrl = configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return ConvertDatabaseUrl(databaseUrl);
        }

        var postgresHost = FirstConfiguredValue(configuration, "POSTGRES_HOST", "Postgres:Host");
        if (!string.IsNullOrWhiteSpace(postgresHost))
        {
            return BuildFromPostgresSettings(configuration, postgresHost);
        }

        return configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("The database connection string is missing.");
    }

    private static string BuildFromPostgresSettings(IConfiguration configuration, string host)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = GetConfiguredInt(configuration, 5432, "POSTGRES_PORT", "Postgres:Port"),
            Database = FirstConfiguredValue(configuration, "POSTGRES_DB", "Postgres:Database")
                ?? throw new InvalidOperationException("The PostgreSQL database name is missing."),
            Username = FirstConfiguredValue(configuration, "POSTGRES_USER", "Postgres:Username")
                ?? throw new InvalidOperationException("The PostgreSQL username is missing."),
            Password = FirstConfiguredValue(configuration, "POSTGRES_PASSWORD", "Postgres:Password") ?? string.Empty
        };

        var sslMode = FirstConfiguredValue(configuration, "POSTGRES_SSL_MODE", "Postgres:SslMode");
        if (!string.IsNullOrWhiteSpace(sslMode) && TryParseSslMode(sslMode, out var parsedSslMode))
        {
            builder.SslMode = parsedSslMode;
        }

        return builder.ConnectionString;
    }

    private static string ConvertDatabaseUrl(string databaseUrl)
    {
        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri) || !IsPostgresUri(uri))
        {
            return databaseUrl;
        }

        var credentials = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = credentials.Length > 0 ? Uri.UnescapeDataString(credentials[0]) : string.Empty,
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty
        };

        ApplySslOptions(builder, uri);

        return builder.ConnectionString;
    }

    private static bool IsPostgresUri(Uri uri)
    {
        return string.Equals(uri.Scheme, "postgresql", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, "postgres", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplySslOptions(NpgsqlConnectionStringBuilder builder, Uri uri)
    {
        var queryParameters = ParseQuery(uri.Query);

        if (queryParameters.TryGetValue("sslmode", out var sslMode)
            && TryParseSslMode(sslMode, out var parsedSslMode))
        {
            builder.SslMode = parsedSslMode;
            return;
        }

        if (queryParameters.TryGetValue("ssl", out var ssl)
            && bool.TryParse(ssl, out var sslEnabled)
            && sslEnabled)
        {
            builder.SslMode = SslMode.Require;
        }
    }

    private static bool TryParseSslMode(string value, out SslMode sslMode)
    {
        var normalizedValue = value.Replace("-", string.Empty, StringComparison.Ordinal);
        return Enum.TryParse(normalizedValue, ignoreCase: true, out sslMode);
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
        {
            return parameters;
        }

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            parameters[key] = value;
        }

        return parameters;
    }

    private static string? FirstConfiguredValue(IConfiguration configuration, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static int GetConfiguredInt(IConfiguration configuration, int defaultValue, params string[] keys)
    {
        var value = FirstConfiguredValue(configuration, keys);
        return int.TryParse(value, out var parsedValue) ? parsedValue : defaultValue;
    }
}
