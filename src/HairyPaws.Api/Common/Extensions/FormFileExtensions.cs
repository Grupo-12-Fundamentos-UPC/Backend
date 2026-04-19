using FluentValidation;
using FluentValidation.Results;
using HairyPaws.Application.Common.Files;

namespace HairyPaws.Api.Common.Extensions;

public static class FormFileExtensions
{
    public static async Task<UploadedFile> ToUploadedFileAsync(this IFormFile? file, string propertyName, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            throw new ValidationException([new ValidationFailure(propertyName, "The file is required.")]);
        }

        await using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);

        return new UploadedFile(
            file.FileName,
            file.ContentType,
            memoryStream.ToArray());
    }
}
