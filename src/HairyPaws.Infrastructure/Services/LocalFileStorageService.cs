using HairyPaws.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace HairyPaws.Infrastructure.Services;

public sealed class LocalFileStorageService(IWebHostEnvironment environment) : IFileStorageService
{
    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken)
    {
        var relativePath = NormalizeRelativePath(fileName);
        var uploadsRoot = GetUploadsRoot();
        var fullPath = Path.Combine(uploadsRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directoryPath = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return $"/uploads/{relativePath}";
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        var fullPath = ResolveFullPath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string GetUploadsRoot() => Path.Combine(environment.ContentRootPath, "uploads");

    private string ResolveFullPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        if (Path.IsPathRooted(path))
        {
            return path;
        }

        var relativePath = NormalizeRelativePath(path);
        return Path.Combine(GetUploadsRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string NormalizeRelativePath(string path)
    {
        var normalizedPath = path.Trim().Replace('\\', '/').TrimStart('/');

        if (normalizedPath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = normalizedPath["uploads/".Length..];
        }

        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            throw new InvalidOperationException("A relative file path is required.");
        }

        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Any(static segment => segment == ".."))
        {
            throw new InvalidOperationException("Path traversal is not allowed.");
        }

        return string.Join('/', segments);
    }
}
