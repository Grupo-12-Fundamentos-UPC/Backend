using HairyPaws.Domain.Adoption.Enums;
using HairyPaws.Domain.Donations.Enums;
using HairyPaws.Domain.Events.Enums;
using HairyPaws.Domain.Identity.Enums;
using HairyPaws.Domain.Notifications.Enums;
using HairyPaws.Domain.Organizations.Enums;
using HairyPaws.Domain.Pets.Enums;
using HairyPaws.Domain.Visits.Enums;

namespace HairyPaws.Application.Common.Mappings;

public static class ContractEnumMapper
{
    public static UserRole ToUserRole(string value) => ParseEnum<UserRole>(value);

    public static UserStatus ToUserStatus(string value) => ParseEnum<UserStatus>(value);

    public static VerificationStatus ToVerificationStatus(string value) => ParseEnum<VerificationStatus>(value);

    public static OrganizationDocumentType ToOrganizationDocumentType(string value) => ParseEnum<OrganizationDocumentType>(value);

    public static AdoptionRequestStatus ToAdoptionRequestStatus(string value) => ParseEnum<AdoptionRequestStatus>(value);

    public static DonationType ToDonationType(string value) => ParseEnum<DonationType>(value);

    public static DonationStatus ToDonationStatus(string value) => ParseEnum<DonationStatus>(value);

    public static EventStatus ToEventStatus(string value) => ParseEnum<EventStatus>(value);

    public static NotificationType ToNotificationType(string value) => ParseEnum<NotificationType>(value);

    public static PetSpecies ToPetSpecies(string value) => ParseEnum<PetSpecies>(value);

    public static PetSex ToPetSex(string value) => ParseEnum<PetSex>(value);

    public static PetSize ToPetSize(string value) => ParseEnum<PetSize>(value);

    public static PetStatus ToPetStatus(string value) => ParseEnum<PetStatus>(value);

    public static VisitStatus ToVisitStatus(string value) => ParseEnum<VisitStatus>(value);

    private static TEnum ParseEnum<TEnum>(string value)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, true, out var parsedValue))
        {
            return parsedValue;
        }

        throw new InvalidOperationException($"Value '{value}' is not valid for {typeof(TEnum).Name}.");
    }
}
