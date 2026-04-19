using HairyPaws.Contracts.Organizations.Responses;
using HairyPaws.Domain.Organizations.Entities;

namespace HairyPaws.Application.Common.Mappings;

public static class OrganizationResponseMappings
{
    public static OrganizationSummaryResponse ToSummaryResponse(this Organization organization)
    {
        return new OrganizationSummaryResponse(
            organization.Id,
            organization.Name,
            organization.Ruc,
            organization.Description,
            organization.Address,
            organization.Phone,
            organization.Email,
            organization.LogoPath,
            organization.VerificationStatus.ToString(),
            organization.CreatedAt,
            organization.UpdatedAt);
    }

    public static OrganizationDetailResponse ToDetailResponse(this Organization organization, bool includeDocuments)
    {
        var documents = includeDocuments
            ? organization.Documents
                .OrderBy(document => document.UploadedAt)
                .Select(static document => document.ToResponse())
                .ToArray()
            : [];

        return new OrganizationDetailResponse(
            organization.Id,
            organization.Name,
            organization.Ruc,
            organization.Description,
            organization.Address,
            organization.Phone,
            organization.Email,
            organization.LogoPath,
            organization.VerificationStatus.ToString(),
            documents,
            organization.CreatedAt,
            organization.UpdatedAt);
    }

    public static OrganizationDocumentResponse ToResponse(this OrganizationDocument document)
    {
        return new OrganizationDocumentResponse(
            document.Id,
            document.DocumentType.ToString(),
            document.FilePath,
            document.UploadedAt);
    }
}
