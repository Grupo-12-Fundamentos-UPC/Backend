namespace HairyPaws.Api.Models;

public sealed class UploadOrganizationDocumentRequest
{
    public string DocumentType { get; init; } = string.Empty;

    public IFormFile? File { get; init; }
}
