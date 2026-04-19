using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HairyPaws.Contracts.Adoption.Responses;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Identity.Responses;
using HairyPaws.Contracts.Pets.Responses;
using HairyPaws.Contracts.Visits.Responses;
using HairyPaws.Tests.Integration.Common;

namespace HairyPaws.Tests.Integration.Visits;

public sealed class VisitsEndpointsTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PetManager_CanCreateVisit_ForRequestUnderReview()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        var scenario = await CreateUnderReviewRequestAsync(ownerClient, adopterClient, "visit-create-owner", "visit-create-adopter");

        var response = await ownerClient.PostAsJsonAsync(
            $"/api/v1/adoption-requests/{scenario.AdoptionRequest.Id}/visits",
            new
            {
                scheduledAt = DateTimeOffset.UtcNow.AddDays(2),
                location = "Shelter main office",
                notes = "Please confirm"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<VisitResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Pending");
        body.AdoptionRequestId.Should().Be(scenario.AdoptionRequest.Id);
    }

    [Fact]
    public async Task CannotCreateVisit_ForInvalidRequestStatus()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        var pet = await CreateAvailablePetForOwnerAsync(ownerClient, "visit-invalid-owner");
        var adoptionRequest = await CreateSubmittedRequestAsync(adopterClient, pet.Id, "visit-invalid-adopter");

        var response = await ownerClient.PostAsJsonAsync(
            $"/api/v1/adoption-requests/{adoptionRequest.Id}/visits",
            new
            {
                scheduledAt = DateTimeOffset.UtcNow.AddDays(2),
                location = "Shelter main office",
                notes = "Should fail"
            });

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("BUSINESS_RULE_VIOLATION");
    }

    [Fact]
    public async Task Adopter_CanApproveVisit()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        var scenario = await CreateVisitScenarioAsync(ownerClient, adopterClient, "visit-approve-owner", "visit-approve-adopter");

        var body = await ApiTestHelper.ApproveVisitAsync(adopterClient, scenario.Visit!.Id, "Confirmed");

        body.Status.Should().Be("Approved");
    }

    [Fact]
    public async Task Adopter_CanRejectVisit()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        var scenario = await CreateVisitScenarioAsync(ownerClient, adopterClient, "visit-reject-owner", "visit-reject-adopter");

        var body = await ApiTestHelper.RejectVisitAsync(adopterClient, scenario.Visit!.Id, "Cannot attend");

        body.Status.Should().Be("Rejected");
    }

    [Fact]
    public async Task UnauthorizedUser_CannotApproveOrReject_SomebodyElsesVisit()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();
        using var intruderClient = factory.CreateApiClient();

        var scenario = await CreateVisitScenarioAsync(ownerClient, adopterClient, "visit-block-owner", "visit-block-adopter");
        await AuthenticateAsync(intruderClient, ApiTestHelper.UniqueEmail("visit-block-intruder"), "Adopter");

        var response = await intruderClient.PostAsJsonAsync(
            $"/api/v1/visits/{scenario.Visit!.Id}/approve",
            new { notes = "Should not work" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PetManager_CanCancelVisit()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        var scenario = await CreateVisitScenarioAsync(ownerClient, adopterClient, "visit-cancel-owner", "visit-cancel-adopter");

        var body = await ApiTestHelper.CancelVisitAsync(ownerClient, scenario.Visit!.Id, "Cancelled by manager");

        body.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task PetManager_CanCompleteVisit()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        var scenario = await CreateVisitScenarioAsync(ownerClient, adopterClient, "visit-complete-owner", "visit-complete-adopter");
        await ApiTestHelper.ApproveVisitAsync(adopterClient, scenario.Visit!.Id, "See you there");

        var body = await ApiTestHelper.CompleteVisitAsync(ownerClient, scenario.Visit!.Id, "Visit completed");

        body.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task CannotCompleteRejectedOrCancelledVisit()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        var scenario = await CreateVisitScenarioAsync(ownerClient, adopterClient, "visit-invalid-complete-owner", "visit-invalid-complete-adopter");
        await ApiTestHelper.RejectVisitAsync(adopterClient, scenario.Visit!.Id, "No longer interested");

        var response = await ownerClient.PostAsJsonAsync(
            $"/api/v1/visits/{scenario.Visit!.Id}/complete",
            new { notes = "Should fail" });

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("BUSINESS_RULE_VIOLATION");
    }

    [Fact]
    public async Task AuthorizedUsers_CanQueryVisitDetails()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();

        var scenario = await CreateVisitScenarioAsync(ownerClient, adopterClient, "visit-query-owner", "visit-query-adopter");

        var detailResponse = await ownerClient.GetAsync($"/api/v1/visits/{scenario.Visit!.Id}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailBody = await detailResponse.Content.ReadFromJsonAsync<VisitResponse>();
        detailBody.Should().NotBeNull();
        detailBody!.Id.Should().Be(scenario.Visit!.Id);

        var listResponse = await adopterClient.GetAsync($"/api/v1/adoption-requests/{scenario.AdoptionRequest.Id}/visits?page=1&pageSize=10");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listBody = await listResponse.Content.ReadFromJsonAsync<PagedResponse<VisitListItemResponse>>();
        listBody.Should().NotBeNull();
        listBody!.Items.Should().Contain(item => item.Id == scenario.Visit!.Id);
    }

    [Fact]
    public async Task UnauthorizedUser_CannotQueryVisitDetails()
    {
        using var ownerClient = factory.CreateApiClient();
        using var adopterClient = factory.CreateApiClient();
        using var intruderClient = factory.CreateApiClient();

        var scenario = await CreateVisitScenarioAsync(ownerClient, adopterClient, "visit-query-block-owner", "visit-query-block-adopter");
        await AuthenticateAsync(intruderClient, ApiTestHelper.UniqueEmail("visit-query-block-other"), "Adopter");

        var response = await intruderClient.GetAsync($"/api/v1/visits/{scenario.Visit!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

    private static async Task<VisitScenario> CreateUnderReviewRequestAsync(
        HttpClient ownerClient,
        HttpClient adopterClient,
        string ownerEmailPrefix,
        string adopterEmailPrefix)
    {
        var pet = await CreateAvailablePetForOwnerAsync(ownerClient, ownerEmailPrefix);
        var adoptionRequest = await CreateSubmittedRequestAsync(adopterClient, pet.Id, adopterEmailPrefix);
        var reviewedRequest = await ApiTestHelper.StartReviewAsync(ownerClient, adoptionRequest.Id);

        return new VisitScenario(pet, reviewedRequest, null);
    }

    private static async Task<VisitScenario> CreateVisitScenarioAsync(
        HttpClient ownerClient,
        HttpClient adopterClient,
        string ownerEmailPrefix,
        string adopterEmailPrefix)
    {
        var scenario = await CreateUnderReviewRequestAsync(ownerClient, adopterClient, ownerEmailPrefix, adopterEmailPrefix);
        var visit = await ApiTestHelper.CreateVisitAsync(ownerClient, scenario.AdoptionRequest.Id);
        return scenario with { Visit = visit };
    }

    private sealed record VisitScenario(
        PetDetailResponse Pet,
        AdoptionRequestDetailResponse AdoptionRequest,
        VisitResponse? Visit);
}
