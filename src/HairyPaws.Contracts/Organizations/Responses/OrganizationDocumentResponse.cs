namespace HairyPaws.Contracts.Organizations.Responses;

public sealed record OrganizationDocumentResponse(
    Guid Id,
    string DocumentType,
    string FilePath,
    DateTimeOffset UploadedAt);
