using HairyPaws.Contracts.Users.Responses;
using HairyPaws.Domain.Identity.Entities;

namespace HairyPaws.Application.Common.Mappings;

public static class UserResponseMappings
{
    public static UserSummaryResponse ToSummaryResponse(this User user)
    {
        return new UserSummaryResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            user.Status.ToString(),
            user.VerificationStatus.ToString(),
            user.CreatedAt,
            user.UpdatedAt);
    }

    public static UserProfileResponse ToProfileResponse(this User user)
    {
        return new UserProfileResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            user.Status.ToString(),
            user.VerificationStatus.ToString(),
            user.PhoneNumber,
            user.IdentityDocument,
            user.Address,
            user.ProfileImagePath,
            user.CreatedAt,
            user.UpdatedAt);
    }
}
