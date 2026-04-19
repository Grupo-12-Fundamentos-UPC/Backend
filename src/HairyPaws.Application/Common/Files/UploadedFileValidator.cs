using FluentValidation;
using FluentValidation.Results;

namespace HairyPaws.Application.Common.Files;

public static class UploadedFileValidator
{
    public static void EnsureImageIsValid(UploadedFile file, string propertyName, long maxBytes)
    {
        EnsureValid(
            file,
            propertyName,
            maxBytes,
            [".jpg", ".jpeg", ".png"],
            ["image/jpeg", "image/png", "image/jpg"]);
    }

    public static void EnsureDocumentIsValid(UploadedFile file, string propertyName, long maxBytes)
    {
        EnsureValid(
            file,
            propertyName,
            maxBytes,
            [".pdf", ".jpg", ".jpeg", ".png"],
            ["application/pdf", "image/jpeg", "image/png", "image/jpg"]);
    }

    public static string GetRequiredExtension(UploadedFile file, string propertyName, params string[] allowedExtensions)
    {
        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw CreateValidationException(propertyName, $"The file extension must be one of: {string.Join(", ", allowedExtensions)}.");
        }

        return extension;
    }

    private static void EnsureValid(
        UploadedFile file,
        string propertyName,
        long maxBytes,
        string[] allowedExtensions,
        string[] allowedContentTypes)
    {
        if (file.Content.Length == 0)
        {
            throw CreateValidationException(propertyName, "The file must not be empty.");
        }

        if (file.Length > maxBytes)
        {
            throw CreateValidationException(propertyName, $"The file size must be {maxBytes / (1024 * 1024)} MB or less.");
        }

        GetRequiredExtension(file, propertyName, allowedExtensions);

        if (!string.IsNullOrWhiteSpace(file.ContentType) &&
            !allowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw CreateValidationException(propertyName, "The file content type is not allowed.");
        }
    }

    private static ValidationException CreateValidationException(string propertyName, string errorMessage)
    {
        return new ValidationException([new ValidationFailure(propertyName, errorMessage)]);
    }
}
