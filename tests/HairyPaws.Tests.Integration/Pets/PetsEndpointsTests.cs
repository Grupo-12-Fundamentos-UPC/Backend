using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Organizations.Responses;
using HairyPaws.Contracts.Pets.Requests;
using HairyPaws.Contracts.Pets.Responses;
using HairyPaws.Tests.Integration.Common;

namespace HairyPaws.Tests.Integration.Pets;

public sealed class PetsEndpointsTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task OwnerUser_CanCreatePersonalPetAsDraft()
    {
        using var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail("owner-pet"), "Owner");
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);

        var response = await client.PostAsJsonAsync("/api/v1/pets", ApiTestHelper.CreatePetRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PetDetailResponse>();
        body!.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task OngUserWithOrganization_CanCreatePetLinkedToOrganization()
    {
        using var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail("ong-pet"), "Ong");
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);
        await CreateOrganizationAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/pets", ApiTestHelper.CreatePetRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PetDetailResponse>();
        body!.Status.Should().Be("Draft");

        var myPetsResponse = await client.GetAsync("/api/v1/pets/mine");
        myPetsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var myPets = await myPetsResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<PetListItemResponse>>();
        myPets.Should().ContainSingle(item => item.Id == body.Id);
    }

    [Fact]
    public async Task PublicCatalog_ReturnsOnlyAvailablePets()
    {
        using var ownerClient = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(ownerClient, ApiTestHelper.UniqueEmail("catalog-owner"), "Owner");
        PostgresWebApplicationFactory.SetBearerToken(ownerClient, login.AccessToken);

        var availablePet = await CreatePetAsync(ownerClient);
        await UploadPhotoAsync(ownerClient, availablePet.Id);
        var publishResponse = await ownerClient.PostAsync($"/api/v1/pets/{availablePet.Id}/publish", content: null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var draftPet = await CreatePetAsync(ownerClient);

        using var anonymousClient = factory.CreateApiClient();
        var catalogResponse = await anonymousClient.GetAsync("/api/v1/pets");

        catalogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await catalogResponse.Content.ReadFromJsonAsync<PagedResponse<PetListItemResponse>>();
        body.Should().NotBeNull();
        body!.Items.Should().Contain(item => item.Id == availablePet.Id);
        body.Items.Should().NotContain(item => item.Id == draftPet.Id);
    }

    [Fact]
    public async Task PetOwner_CanUpdatePet()
    {
        using var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail("update-pet"), "Owner");
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);
        var pet = await CreatePetAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/pets/{pet.Id}",
            new UpdatePetRequest
            {
                Name = "Updated Milo",
                Description = "Updated description"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PetDetailResponse>();
        body!.Name.Should().Be("Updated Milo");
        body.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task NonOwner_CannotUpdatePet()
    {
        using var ownerClient = factory.CreateApiClient();
        var ownerLogin = await ApiTestHelper.RegisterAndLoginAsync(ownerClient, ApiTestHelper.UniqueEmail("pet-owner"), "Owner");
        PostgresWebApplicationFactory.SetBearerToken(ownerClient, ownerLogin.AccessToken);
        var pet = await CreatePetAsync(ownerClient);

        using var anotherClient = factory.CreateApiClient();
        var anotherLogin = await ApiTestHelper.RegisterAndLoginAsync(anotherClient, ApiTestHelper.UniqueEmail("pet-other"), "Owner");
        PostgresWebApplicationFactory.SetBearerToken(anotherClient, anotherLogin.AccessToken);

        var response = await anotherClient.PutAsJsonAsync(
            $"/api/v1/pets/{pet.Id}",
            new UpdatePetRequest { Name = "Should Not Work" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PetOwner_CanUploadPhoto()
    {
        using var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail("pet-photo"), "Owner");
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);
        var pet = await CreatePetAsync(client);

        var photo = await UploadPhotoAsync(client, pet.Id);

        photo.FilePath.Should().StartWith("/uploads/pets/photos/");
    }

    [Fact]
    public async Task Publish_ShouldFail_WhenNoPhotoExists()
    {
        using var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail("pet-publish-fail"), "Owner");
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);
        var pet = await CreatePetAsync(client);

        var response = await client.PostAsync($"/api/v1/pets/{pet.Id}/publish", content: null);

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("BUSINESS_RULE_VIOLATION");
    }

    [Fact]
    public async Task Publish_ShouldSucceed_WhenMinimumRequirementsAreMet()
    {
        using var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail("pet-publish-success"), "Owner");
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);
        var pet = await CreatePetAsync(client);
        await UploadPhotoAsync(client, pet.Id);

        var response = await client.PostAsync($"/api/v1/pets/{pet.Id}/publish", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PetDetailResponse>();
        body!.Status.Should().Be("Available");
        body.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Archive_ShouldSucceed_FromDraftOrAvailable()
    {
        using var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail("pet-archive"), "Owner");
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);

        var draftPet = await CreatePetAsync(client);
        var archiveDraftResponse = await client.PostAsync($"/api/v1/pets/{draftPet.Id}/archive", content: null);
        archiveDraftResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var archivedDraftBody = await archiveDraftResponse.Content.ReadFromJsonAsync<PetDetailResponse>();
        archivedDraftBody!.Status.Should().Be("Archived");

        var availablePet = await CreatePetAsync(client);
        await UploadPhotoAsync(client, availablePet.Id);
        await client.PostAsync($"/api/v1/pets/{availablePet.Id}/publish", content: null);

        var archiveAvailableResponse = await client.PostAsync($"/api/v1/pets/{availablePet.Id}/archive", content: null);
        archiveAvailableResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var archivedAvailableBody = await archiveAvailableResponse.Content.ReadFromJsonAsync<PetDetailResponse>();
        archivedAvailableBody!.Status.Should().Be("Archived");
    }

    [Fact]
    public async Task UnauthorizedUser_CannotManageAnotherUsersPet()
    {
        using var ownerClient = factory.CreateApiClient();
        var ownerLogin = await ApiTestHelper.RegisterAndLoginAsync(ownerClient, ApiTestHelper.UniqueEmail("pet-owner-manage"), "Owner");
        PostgresWebApplicationFactory.SetBearerToken(ownerClient, ownerLogin.AccessToken);
        var pet = await CreatePetAsync(ownerClient);

        using var intruderClient = factory.CreateApiClient();
        var intruderLogin = await ApiTestHelper.RegisterAndLoginAsync(intruderClient, ApiTestHelper.UniqueEmail("pet-intruder"), "Owner");
        PostgresWebApplicationFactory.SetBearerToken(intruderClient, intruderLogin.AccessToken);
        using var content = ApiTestHelper.CreateImageUpload();

        var response = await intruderClient.PostAsync($"/api/v1/pets/{pet.Id}/photos", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<OrganizationDetailResponse> CreateOrganizationAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/organizations", ApiTestHelper.CreateOrganizationRequest());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<OrganizationDetailResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    private static async Task<PetDetailResponse> CreatePetAsync(HttpClient client, CreatePetRequest? request = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/pets", request ?? ApiTestHelper.CreatePetRequest());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PetDetailResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    private static async Task<PetPhotoResponse> UploadPhotoAsync(HttpClient client, Guid petId)
    {
        using var content = ApiTestHelper.CreateImageUpload();
        var response = await client.PostAsync($"/api/v1/pets/{petId}/photos", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PetPhotoResponse>();
        body.Should().NotBeNull();
        return body!;
    }
}
