using HairyPaws.Contracts.Adoption.Requests;
using HairyPaws.Contracts.Adoption.Responses;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using HairyPaws.Contracts.Donations.Requests;
using HairyPaws.Contracts.Donations.Responses;
using HairyPaws.Contracts.Events.Requests;
using HairyPaws.Contracts.Events.Responses;
using FluentAssertions;
using HairyPaws.Contracts.Identity.Requests;
using HairyPaws.Contracts.Identity.Responses;
using HairyPaws.Contracts.Notifications.Responses;
using HairyPaws.Contracts.Organizations.Requests;
using HairyPaws.Contracts.Organizations.Responses;
using HairyPaws.Contracts.Pets.Requests;
using HairyPaws.Contracts.Pets.Responses;
using HairyPaws.Contracts.Visits.Requests;
using HairyPaws.Contracts.Visits.Responses;

namespace HairyPaws.Tests.Integration.Common;

internal static class ApiTestHelper
{
    public static RegisterRequest CreateRegisterRequest(string email, string role)
    {
        return new RegisterRequest
        {
            Email = email,
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
            Role = role,
            PhoneNumber = "5551234",
            Address = "Test Address"
        };
    }

    public static async Task<AuthResponse> RegisterAndLoginAsync(HttpClient client, string email, string role)
    {
        var registerRequest = CreateRegisterRequest(email, role);
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        return await LoginAsync(client, email, registerRequest.Password);
    }

    public static async Task<AuthResponse> LoginAsync(HttpClient client, string email, string password = "Password123!")
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest { Email = email, Password = password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static MultipartFormDataContent CreateImageUpload(string fieldName = "file", string fileName = "image.jpg")
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("fake-image-content"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, fieldName, fileName);
        return content;
    }

    public static MultipartFormDataContent CreateDocumentUpload(
        string documentType,
        string fieldName = "file",
        string fileName = "document.pdf")
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake-pdf-content"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, fieldName, fileName);
        content.Add(new StringContent(documentType), "documentType");
        return content;
    }

    public static MultipartFormDataContent CreateReceiptUpload(
        string fieldName = "file",
        string fileName = "receipt.pdf")
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake-receipt-content"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, fieldName, fileName);
        return content;
    }

    public static CreateOrganizationRequest CreateOrganizationRequest(string? ruc = null, string? name = null)
    {
        return new CreateOrganizationRequest
        {
            Name = name ?? "Happy Tails ONG",
            Ruc = ruc ?? GenerateRuc(),
            Description = "Local rescue organization",
            Address = "123 Pet Street",
            Phone = "5556789",
            Email = "org@hairypaws.test"
        };
    }

    public static CreatePetRequest CreatePetRequest(
        string species = "Dog",
        string sex = "Male",
        string size = "Medium",
        string? description = "Friendly and playful",
        string? locationDistrict = "Miraflores")
    {
        return new CreatePetRequest
        {
            Name = "Milo",
            Species = species,
            Breed = "Mixed",
            AgeText = "2 years",
            Sex = sex,
            Size = size,
            Sterilized = true,
            Vaccinated = true,
            Description = description,
            Temperament = "Calm",
            MedicalHistory = "Healthy",
            LocationDistrict = locationDistrict
        };
    }

    public static string GenerateRuc()
    {
        var digits = Guid.NewGuid().ToString("N")[..11];
        var numericOnly = new string(digits.Select(static character => char.IsDigit(character) ? character : '7').ToArray());
        return numericOnly;
    }

    public static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@hairypaws.test";

    public static CreateDonationRequest CreateMoneyDonationRequest(Guid organizationId, decimal amount = 50)
    {
        return new CreateDonationRequest
        {
            OrganizationId = organizationId,
            DonationType = "Money",
            Amount = amount,
            TransactionId = $"txn-{Guid.NewGuid():N}"[..20],
            Notes = "Donation for food and care."
        };
    }

    public static CreateDonationRequest CreateItemsDonationRequest(Guid organizationId)
    {
        return new CreateDonationRequest
        {
            OrganizationId = organizationId,
            DonationType = "Items",
            Notes = "Donation with supplies.",
            Items =
            [
                new CreateDonationItemRequest
                {
                    Name = "Dog Food",
                    Quantity = 3,
                    Description = "15kg bags"
                }
            ]
        };
    }

    public static CreateEventRequest CreateEventRequest(Guid organizationId, DateTimeOffset? eventDate = null)
    {
        return new CreateEventRequest
        {
            OrganizationId = organizationId,
            Title = "Vaccination Campaign",
            Description = "Community pet care event.",
            EventDate = eventDate ?? DateTimeOffset.UtcNow.AddDays(10),
            Location = "Central Park",
            IsVolunteerEvent = true
        };
    }

    public static async Task<OrganizationDetailResponse> CreateOrganizationAsync(HttpClient client, CreateOrganizationRequest? request = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/organizations", request ?? CreateOrganizationRequest());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<OrganizationDetailResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<PetDetailResponse> CreatePetAsync(HttpClient client, CreatePetRequest? request = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/pets", request ?? CreatePetRequest());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PetDetailResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<PetPhotoResponse> UploadPhotoAsync(HttpClient client, Guid petId)
    {
        using var content = CreateImageUpload();
        var response = await client.PostAsync($"/api/v1/pets/{petId}/photos", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PetPhotoResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<PetDetailResponse> PublishPetAsync(HttpClient client, Guid petId)
    {
        var response = await client.PostAsync($"/api/v1/pets/{petId}/publish", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PetDetailResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<PetDetailResponse> CreateAvailablePetAsync(HttpClient client, CreatePetRequest? request = null)
    {
        var pet = await CreatePetAsync(client, request);
        await UploadPhotoAsync(client, pet.Id);
        return await PublishPetAsync(client, pet.Id);
    }

    public static async Task<DonationResponse> CreateDonationAsync(HttpClient client, CreateDonationRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/v1/donations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<DonationResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<DonationResponse> ConfirmDonationAsync(HttpClient client, Guid donationId)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/donations/{donationId}/confirm", new ConfirmDonationRequest());
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DonationResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<DonationResponse> CancelDonationAsync(HttpClient client, Guid donationId)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/donations/{donationId}/cancel", new CancelDonationRequest());
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DonationResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<EventDetailResponse> CreateEventAsync(HttpClient client, CreateEventRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/v1/events", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<EventDetailResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<EventDetailResponse> PublishEventAsync(HttpClient client, Guid eventId)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/events/{eventId}/publish", new PublishEventRequest());
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EventDetailResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<EventDetailResponse> CancelEventAsync(HttpClient client, Guid eventId)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/events/{eventId}/cancel", new CancelEventRequest());
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EventDetailResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<UnreadNotificationsCountResponse> GetUnreadNotificationsCountAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/notifications/unread-count");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UnreadNotificationsCountResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static SubmitAdoptionRequestRequest CreateSubmitAdoptionRequest(Guid petId)
    {
        return new SubmitAdoptionRequestRequest
        {
            PetId = petId,
            ContactPhone = "5554444",
            LivingConditions = "Apartment with balcony",
            HasPreviousPets = true,
            WhyAdopt = "Ready to provide a stable home"
        };
    }

    public static async Task<AdoptionRequestDetailResponse> SubmitAdoptionRequestAsync(
        HttpClient client,
        Guid petId,
        SubmitAdoptionRequestRequest? request = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/adoption-requests", request ?? CreateSubmitAdoptionRequest(petId));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AdoptionRequestDetailResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<AdoptionRequestDetailResponse> StartReviewAsync(HttpClient client, Guid adoptionRequestId)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/adoption-requests/{adoptionRequestId}/start-review",
            new StartAdoptionReviewRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AdoptionRequestDetailResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<AdoptionRequestDetailResponse> ApproveAdoptionRequestAsync(HttpClient client, Guid adoptionRequestId, string? notes = null)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/adoption-requests/{adoptionRequestId}/approve",
            new ApproveAdoptionRequestRequest
            {
                Notes = notes
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AdoptionRequestDetailResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<AdoptionRequestDetailResponse> RejectAdoptionRequestAsync(HttpClient client, Guid adoptionRequestId, string? notes = null)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/adoption-requests/{adoptionRequestId}/reject",
            new RejectAdoptionRequestRequest
            {
                Notes = notes
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AdoptionRequestDetailResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<AdoptionRequestDetailResponse> CancelAdoptionRequestAsync(HttpClient client, Guid adoptionRequestId, string? notes = null)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/adoption-requests/{adoptionRequestId}/cancel",
            new CancelAdoptionRequestRequest
            {
                Notes = notes
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AdoptionRequestDetailResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<AdoptionRequestDetailResponse> CompleteAdoptionRequestAsync(HttpClient client, Guid adoptionRequestId, string? notes = null)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/adoption-requests/{adoptionRequestId}/complete",
            new CompleteAdoptionRequestRequest
            {
                Notes = notes
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AdoptionRequestDetailResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<VisitResponse> CreateVisitAsync(HttpClient client, Guid adoptionRequestId, DateTimeOffset? scheduledAt = null)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/adoption-requests/{adoptionRequestId}/visits",
            new CreateVisitRequest
            {
                ScheduledAt = scheduledAt ?? DateTimeOffset.UtcNow.AddDays(2),
                Location = "Veterinary Office",
                Notes = "Please confirm availability"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<VisitResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<VisitResponse> ApproveVisitAsync(HttpClient client, Guid visitId, string? notes = null)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/visits/{visitId}/approve",
            new ApproveVisitRequest
            {
                Notes = notes
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<VisitResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<VisitResponse> RejectVisitAsync(HttpClient client, Guid visitId, string? notes = null)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/visits/{visitId}/reject",
            new RejectVisitRequest
            {
                Notes = notes
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<VisitResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<VisitResponse> CancelVisitAsync(HttpClient client, Guid visitId, string? notes = null)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/visits/{visitId}/cancel",
            new CancelVisitRequest
            {
                Notes = notes
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<VisitResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<VisitResponse> CompleteVisitAsync(HttpClient client, Guid visitId, string? notes = null)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/visits/{visitId}/complete",
            new CompleteVisitRequest
            {
                Notes = notes
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<VisitResponse>();
        body.Should().NotBeNull();
        return body!;
    }
}
