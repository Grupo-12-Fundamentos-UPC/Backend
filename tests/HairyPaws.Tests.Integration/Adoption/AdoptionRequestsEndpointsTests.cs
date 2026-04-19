using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HairyPaws.Contracts.Adoption.Responses;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Identity.Responses;
using HairyPaws.Contracts.Pets.Responses;
using HairyPaws.Domain.Pets.Entities;
using HairyPaws.Domain.Pets.Enums;
using HairyPaws.Tests.Integration.Common;

namespace HairyPaws.Tests.Integration.Adoption;

public sealed class AdoptionRequestsEndpointsTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Adopter_CanSubmitRequest_ForAvailablePet()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        await AuthenticateAsync(ownerClient, ApiTestHelper.UniqueEmail("adoption-owner"), "Owner");
        var pet = await ApiTestHelper.CreateAvailablePetAsync(ownerClient);

        var adopterLogin = await AuthenticateAsync(adopterClient, ApiTestHelper.UniqueEmail("adoption-adopter"), "Adopter");
        var response = await adopterClient.PostAsJsonAsync("/api/v1/adoption-requests", ApiTestHelper.CreateSubmitAdoptionRequest(pet.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AdoptionRequestDetailResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Submitted");
        body.Pet.Id.Should().Be(pet.Id);
        body.Adopter.Id.Should().Be(adopterLogin.User.Id);
    }

    [Fact]
    public async Task NonAdopter_CannotSubmitRequest()
    {
        using var ownerClient = factory.CreateApiClient();
        using var requesterClient = factory.CreateApiClient();

        await AuthenticateAsync(ownerClient, ApiTestHelper.UniqueEmail("adoption-owner"), "Owner");
        var pet = await ApiTestHelper.CreateAvailablePetAsync(ownerClient);

        await AuthenticateAsync(requesterClient, ApiTestHelper.UniqueEmail("adoption-non-adopter"), "Owner");
        var response = await requesterClient.PostAsJsonAsync("/api/v1/adoption-requests", ApiTestHelper.CreateSubmitAdoptionRequest(pet.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CannotSubmitRequest_ForNonAvailablePet()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        await AuthenticateAsync(ownerClient, ApiTestHelper.UniqueEmail("draft-owner"), "Owner");
        var draftPet = await ApiTestHelper.CreatePetAsync(ownerClient);

        await AuthenticateAsync(adopterClient, ApiTestHelper.UniqueEmail("draft-adopter"), "Adopter");
        var response = await adopterClient.PostAsJsonAsync("/api/v1/adoption-requests", ApiTestHelper.CreateSubmitAdoptionRequest(draftPet.Id));

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("BUSINESS_RULE_VIOLATION");
    }

    [Fact]
    public async Task CannotSubmitDuplicateActiveRequest_ForSamePet()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        await AuthenticateAsync(ownerClient, ApiTestHelper.UniqueEmail("duplicate-owner"), "Owner");
        var pet = await ApiTestHelper.CreateAvailablePetAsync(ownerClient);

        await AuthenticateAsync(adopterClient, ApiTestHelper.UniqueEmail("duplicate-adopter"), "Adopter");
        await adopterClient.PostAsJsonAsync("/api/v1/adoption-requests", ApiTestHelper.CreateSubmitAdoptionRequest(pet.Id));

        var response = await adopterClient.PostAsJsonAsync("/api/v1/adoption-requests", ApiTestHelper.CreateSubmitAdoptionRequest(pet.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("CONFLICT");
    }

    [Fact]
    public async Task CannotSubmitRequest_ForOwnPet()
    {
        using var adopterClient = factory.CreateApiClient();

        var adopterLogin = await AuthenticateAsync(adopterClient, ApiTestHelper.UniqueEmail("self-pet"), "Adopter");
        var petId = await CreateAvailablePetOwnedByAsync(adopterLogin.User.Id);

        var response = await adopterClient.PostAsJsonAsync("/api/v1/adoption-requests", ApiTestHelper.CreateSubmitAdoptionRequest(petId));

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("BUSINESS_RULE_VIOLATION");
    }

    [Fact]
    public async Task PetManager_CanListRequests_ForOwnPet()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        var pet = await CreateAvailablePetForOwnerAsync(ownerClient, "list-requests-owner");
        var adoptionRequest = await CreateSubmittedRequestAsync(adopterClient, pet.Id, "list-requests-adopter");

        var response = await ownerClient.GetAsync($"/api/v1/pets/{pet.Id}/adoption-requests?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<AdoptionRequestListItemResponse>>();
        body.Should().NotBeNull();
        body!.Items.Should().Contain(item => item.Id == adoptionRequest.Id);
    }

    [Fact]
    public async Task UnauthorizedUser_CannotListRequests_ForAnotherUsersPet()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();
        using var intruderClient = factory.CreateApiClient();

        var pet = await CreateAvailablePetForOwnerAsync(ownerClient, "list-block-owner");
        await CreateSubmittedRequestAsync(adopterClient, pet.Id, "list-block-adopter");
        await AuthenticateAsync(intruderClient, ApiTestHelper.UniqueEmail("list-block-intruder"), "Owner");

        var response = await intruderClient.GetAsync($"/api/v1/pets/{pet.Id}/adoption-requests?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PetManager_CanStartReview()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        var pet = await CreateAvailablePetForOwnerAsync(ownerClient, "review-owner");
        var adoptionRequest = await CreateSubmittedRequestAsync(adopterClient, pet.Id, "review-adopter");

        var response = await ownerClient.PostAsJsonAsync(
            $"/api/v1/adoption-requests/{adoptionRequest.Id}/start-review",
            new { notes = "Review started" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AdoptionRequestDetailResponse>();
        body!.Status.Should().Be("UnderReview");
    }

    [Fact]
    public async Task PetManager_CanApproveRequest()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        var pet = await CreateAvailablePetForOwnerAsync(ownerClient, "approve-owner");
        var adoptionRequest = await CreateSubmittedRequestAsync(adopterClient, pet.Id, "approve-adopter");

        var body = await ApiTestHelper.ApproveAdoptionRequestAsync(ownerClient, adoptionRequest.Id, "Approved after review");

        body.Status.Should().Be("Approved");
    }

    [Fact]
    public async Task ApprovingRequest_SetsPetToPendingAdoption()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        var pet = await CreateAvailablePetForOwnerAsync(ownerClient, "approve-pet-owner");
        var adoptionRequest = await CreateSubmittedRequestAsync(adopterClient, pet.Id, "approve-pet-adopter");

        var body = await ApiTestHelper.ApproveAdoptionRequestAsync(ownerClient, adoptionRequest.Id);

        body.Pet.Status.Should().Be("PendingAdoption");
    }

    [Fact]
    public async Task ApprovingRequest_ResolvesCompetingActiveRequests()
    {
        using var ownerClient = factory.CreateApiClient();
        using var firstAdopterClient = factory.CreateApiClient();
        using var secondAdopterClient = factory.CreateApiClient();

        var pet = await CreateAvailablePetForOwnerAsync(ownerClient, "competing-owner");
        var firstRequest = await CreateSubmittedRequestAsync(firstAdopterClient, pet.Id, "competing-adopter-one");
        var secondRequest = await CreateSubmittedRequestAsync(secondAdopterClient, pet.Id, "competing-adopter-two");

        await ApiTestHelper.ApproveAdoptionRequestAsync(ownerClient, firstRequest.Id);

        var response = await ownerClient.GetAsync($"/api/v1/pets/{pet.Id}/adoption-requests?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<AdoptionRequestListItemResponse>>();
        body.Should().NotBeNull();
        body!.Items.Should().Contain(item => item.Id == firstRequest.Id && item.Status == "Approved");
        body.Items.Should().Contain(item => item.Id == secondRequest.Id && item.Status == "Rejected");
    }

    [Fact]
    public async Task PetManager_CanRejectRequest()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        var pet = await CreateAvailablePetForOwnerAsync(ownerClient, "reject-owner");
        var adoptionRequest = await CreateSubmittedRequestAsync(adopterClient, pet.Id, "reject-adopter");

        var body = await ApiTestHelper.RejectAdoptionRequestAsync(ownerClient, adoptionRequest.Id, "Rejected");

        body.Status.Should().Be("Rejected");
    }

    [Fact]
    public async Task Adopter_CanCancelOwnRequest()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        var pet = await CreateAvailablePetForOwnerAsync(ownerClient, "cancel-owner");
        var adoptionRequest = await CreateSubmittedRequestAsync(adopterClient, pet.Id, "cancel-adopter");

        var body = await ApiTestHelper.CancelAdoptionRequestAsync(adopterClient, adoptionRequest.Id, "No longer possible");

        body.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task AnotherAdopter_CannotCancelSomebodyElsesRequest()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();
        using var intruderClient = factory.CreateApiClient();

        var pet = await CreateAvailablePetForOwnerAsync(ownerClient, "cancel-block-owner");
        var adoptionRequest = await CreateSubmittedRequestAsync(adopterClient, pet.Id, "cancel-block-adopter");
        await AuthenticateAsync(intruderClient, ApiTestHelper.UniqueEmail("cancel-block-other"), "Adopter");

        var response = await intruderClient.PostAsJsonAsync(
            $"/api/v1/adoption-requests/{adoptionRequest.Id}/cancel",
            new { notes = "Should not work" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PetManager_CanCompleteApprovedRequest()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        var pet = await CreateAvailablePetForOwnerAsync(ownerClient, "complete-owner");
        var adoptionRequest = await CreateSubmittedRequestAsync(adopterClient, pet.Id, "complete-adopter");
        await ApiTestHelper.ApproveAdoptionRequestAsync(ownerClient, adoptionRequest.Id);

        var body = await ApiTestHelper.CompleteAdoptionRequestAsync(ownerClient, adoptionRequest.Id, "Completed");

        body.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task CompletingRequest_SetsPetToAdopted()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        var pet = await CreateAvailablePetForOwnerAsync(ownerClient, "adopted-owner");
        var adoptionRequest = await CreateSubmittedRequestAsync(adopterClient, pet.Id, "adopted-adopter");
        await ApiTestHelper.ApproveAdoptionRequestAsync(ownerClient, adoptionRequest.Id);

        var body = await ApiTestHelper.CompleteAdoptionRequestAsync(ownerClient, adoptionRequest.Id);

        body.Pet.Status.Should().Be("Adopted");
    }

    private static async Task<AuthResponse> AuthenticateAsync(HttpClient client, string email, string role)
    {
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, email, role);
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);
        return login;
    }

    private static async Task<PetDetailResponse> CreateAvailablePetForOwnerAsync(HttpClient ownerClient, string emailPrefix)
    {
        await AuthenticateAsync(ownerClient, ApiTestHelper.UniqueEmail(emailPrefix), "Owner");
        return await ApiTestHelper.CreateAvailablePetAsync(ownerClient);
    }

    private static async Task<AdoptionRequestDetailResponse> CreateSubmittedRequestAsync(HttpClient adopterClient, Guid petId, string emailPrefix)
    {
        await AuthenticateAsync(adopterClient, ApiTestHelper.UniqueEmail(emailPrefix), "Adopter");
        return await ApiTestHelper.SubmitAdoptionRequestAsync(adopterClient, petId);
    }

    private async Task<Guid> CreateAvailablePetOwnedByAsync(Guid userId)
    {
        return await factory.ExecuteDbContextAsync(async dbContext =>
        {
            var utcNow = DateTimeOffset.UtcNow;
            var pet = Pet.CreateForOwner(
                userId,
                "Self Pet",
                PetSpecies.Dog,
                "Mixed",
                "3 years",
                PetSex.Female,
                PetSize.Small,
                sterilized: true,
                vaccinated: true,
                description: "Very sweet dog",
                temperament: "Friendly",
                medicalHistory: null,
                locationDistrict: "Barranco",
                utcNow);

            pet.AddPhoto("/uploads/pets/photos/self-owned.jpg", 1, utcNow);
            pet.Publish(utcNow.AddMinutes(1));

            await dbContext.Pets.AddAsync(pet);
            return pet.Id;
        });
    }
}
