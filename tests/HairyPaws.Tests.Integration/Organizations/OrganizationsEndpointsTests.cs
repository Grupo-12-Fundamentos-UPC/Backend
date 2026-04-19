using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Organizations.Requests;
using HairyPaws.Contracts.Organizations.Responses;
using HairyPaws.Tests.Integration.Common;

namespace HairyPaws.Tests.Integration.Organizations;

public sealed class OrganizationsEndpointsTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task OngUser_CanCreateOrganization()
    {
        using var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail("org-owner"), "Ong");
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);

        var response = await client.PostAsJsonAsync("/api/v1/organizations", ApiTestHelper.CreateOrganizationRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<OrganizationDetailResponse>();
        body.Should().NotBeNull();
        body!.VerificationStatus.Should().Be("Pending");
        body.Name.Should().Be("Happy Tails ONG");
    }

    [Fact]
    public async Task NonOngUser_CannotCreateOrganization()
    {
        using var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail("owner"), "Owner");
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);

        var response = await client.PostAsJsonAsync("/api/v1/organizations", ApiTestHelper.CreateOrganizationRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DuplicateRuc_ShouldFail()
    {
        var duplicatedRuc = ApiTestHelper.GenerateRuc();

        using var firstClient = factory.CreateApiClient();
        var firstLogin = await ApiTestHelper.RegisterAndLoginAsync(firstClient, ApiTestHelper.UniqueEmail("org-one"), "Ong");
        PostgresWebApplicationFactory.SetBearerToken(firstClient, firstLogin.AccessToken);
        await firstClient.PostAsJsonAsync("/api/v1/organizations", ApiTestHelper.CreateOrganizationRequest(duplicatedRuc));

        using var secondClient = factory.CreateApiClient();
        var secondLogin = await ApiTestHelper.RegisterAndLoginAsync(secondClient, ApiTestHelper.UniqueEmail("org-two"), "Ong");
        PostgresWebApplicationFactory.SetBearerToken(secondClient, secondLogin.AccessToken);

        var response = await secondClient.PostAsJsonAsync("/api/v1/organizations", ApiTestHelper.CreateOrganizationRequest(duplicatedRuc));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("CONFLICT");
    }

    [Fact]
    public async Task OngUser_CannotCreateSecondOrganization()
    {
        using var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail("single-org"), "Ong");
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);

        await client.PostAsJsonAsync("/api/v1/organizations", ApiTestHelper.CreateOrganizationRequest());
        var response = await client.PostAsJsonAsync("/api/v1/organizations", ApiTestHelper.CreateOrganizationRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task OrganizationOwner_CanUpdateOrganization()
    {
        using var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail("org-update"), "Ong");
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);
        var organization = await CreateOrganizationAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/organizations/{organization.Id}",
            new UpdateOrganizationRequest
            {
                Name = "Updated Happy Tails ONG",
                Description = "Updated description"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OrganizationDetailResponse>();
        body!.Name.Should().Be("Updated Happy Tails ONG");
        body.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task NonOwner_CannotUpdateOrganization()
    {
        using var ownerClient = factory.CreateApiClient();
        var ownerLogin = await ApiTestHelper.RegisterAndLoginAsync(ownerClient, ApiTestHelper.UniqueEmail("org-owner"), "Ong");
        PostgresWebApplicationFactory.SetBearerToken(ownerClient, ownerLogin.AccessToken);
        var organization = await CreateOrganizationAsync(ownerClient);

        using var anotherClient = factory.CreateApiClient();
        var anotherLogin = await ApiTestHelper.RegisterAndLoginAsync(anotherClient, ApiTestHelper.UniqueEmail("org-other"), "Ong");
        PostgresWebApplicationFactory.SetBearerToken(anotherClient, anotherLogin.AccessToken);

        var response = await anotherClient.PutAsJsonAsync(
            $"/api/v1/organizations/{organization.Id}",
            new UpdateOrganizationRequest { Name = "Should Fail" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_CanVerifyOrganization()
    {
        using var ownerClient = factory.CreateApiClient();
        var ownerLogin = await ApiTestHelper.RegisterAndLoginAsync(ownerClient, ApiTestHelper.UniqueEmail("org-verify"), "Ong");
        PostgresWebApplicationFactory.SetBearerToken(ownerClient, ownerLogin.AccessToken);
        var organization = await CreateOrganizationAsync(ownerClient);

        using var adminClient = factory.CreateApiClient();
        var adminLogin = await ApiTestHelper.LoginAsync(adminClient, factory.AdminEmail, factory.AdminPassword);
        PostgresWebApplicationFactory.SetBearerToken(adminClient, adminLogin.AccessToken);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/organizations/{organization.Id}/verify",
            new VerifyOrganizationRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OrganizationDetailResponse>();
        body!.VerificationStatus.Should().Be("Verified");
    }

    [Fact]
    public async Task Admin_CanRejectOrganization()
    {
        using var ownerClient = factory.CreateApiClient();
        var ownerLogin = await ApiTestHelper.RegisterAndLoginAsync(ownerClient, ApiTestHelper.UniqueEmail("org-reject"), "Ong");
        PostgresWebApplicationFactory.SetBearerToken(ownerClient, ownerLogin.AccessToken);
        var organization = await CreateOrganizationAsync(ownerClient);

        using var adminClient = factory.CreateApiClient();
        var adminLogin = await ApiTestHelper.LoginAsync(adminClient, factory.AdminEmail, factory.AdminPassword);
        PostgresWebApplicationFactory.SetBearerToken(adminClient, adminLogin.AccessToken);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/organizations/{organization.Id}/reject",
            new RejectOrganizationRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OrganizationDetailResponse>();
        body!.VerificationStatus.Should().Be("Rejected");
    }

    [Fact]
    public async Task NonAdmin_CannotVerifyOrganization()
    {
        using var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail("org-non-admin"), "Ong");
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);
        var organization = await CreateOrganizationAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/admin/organizations/{organization.Id}/verify",
            new VerifyOrganizationRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OrganizationOwner_CanUploadLogo()
    {
        using var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail("org-logo"), "Ong");
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);
        var organization = await CreateOrganizationAsync(client);
        using var content = ApiTestHelper.CreateImageUpload();

        var response = await client.PostAsync($"/api/v1/organizations/{organization.Id}/logo", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OrganizationDetailResponse>();
        body!.LogoPath.Should().StartWith("/uploads/organizations/logos/");
    }

    [Fact]
    public async Task OrganizationOwner_CanUploadDocument()
    {
        using var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail("org-doc"), "Ong");
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);
        var organization = await CreateOrganizationAsync(client);
        using var content = ApiTestHelper.CreateDocumentUpload("License");

        var response = await client.PostAsync($"/api/v1/organizations/{organization.Id}/documents", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OrganizationDocumentResponse>();
        body!.DocumentType.Should().Be("License");
        body.FilePath.Should().StartWith("/uploads/organizations/documents/");
    }

    private static async Task<OrganizationDetailResponse> CreateOrganizationAsync(HttpClient client, CreateOrganizationRequest? request = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/organizations", request ?? ApiTestHelper.CreateOrganizationRequest());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<OrganizationDetailResponse>();
        body.Should().NotBeNull();
        return body!;
    }
}
