namespace HairyPaws.Infrastructure.Services;

public sealed class FileStorageOptions
{
    public const string SectionName = "Storage";

    public string? UploadsPath { get; init; }

    public string GetUploadsRoot(string contentRootPath)
    {
        var configuredPath = string.IsNullOrWhiteSpace(UploadsPath)
            ? Path.Combine(contentRootPath, "uploads")
            : Environment.ExpandEnvironmentVariables(UploadsPath.Trim());

        if (Path.IsPathRooted(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        return Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));
    }
}
