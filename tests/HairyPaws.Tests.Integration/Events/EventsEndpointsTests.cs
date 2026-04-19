using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Events.Requests;
using HairyPaws.Contracts.Events.Responses;
using HairyPaws.Contracts.Organizations.Requests;
using HairyPaws.Contracts.Organizations.Responses;
using HairyPaws.Tests.Integration.Common;

namespace HairyPaws.Tests.Integration.Events;

public sealed class EventsEndpointsTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task OrganizationOwner_CanCreateDraftEvent()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("event-create-owner");
        using var organizationOwnerClient = organizationOwner.Client;

        var response = await organizationOwnerClient.PostAsJsonAsync(
            "/api/v1/events",
            ApiTestHelper.CreateEventRequest(organizationOwner.Organization.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<EventDetailResponse>();
        body!.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task NonOwner_CannotCreateOrUpdateEventForAnotherOrganization()
    {
        var owner = await CreateOngOrganizationClientAsync("event-owner");
        using var ownerClient = owner.Client;
        var existingEvent = await ApiTestHelper.CreateEventAsync(ownerClient, ApiTestHelper.CreateEventRequest(owner.Organization.Id));

        var otherOng = await CreateOngOrganizationClientAsync("event-other");
        using var otherOngClient = otherOng.Client;

        var createResponse = await otherOngClient.PostAsJsonAsync(
            "/api/v1/events",
            ApiTestHelper.CreateEventRequest(owner.Organization.Id));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var updateResponse = await otherOngClient.PutAsJsonAsync(
            $"/api/v1/events/{existingEvent.Id}",
            new UpdateEventRequest { Title = "Unauthorized update" });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task VerifiedOrganization_CanPublishEvent()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("event-publish-owner");
        using var organizationOwnerClient = organizationOwner.Client;
        await VerifyOrganizationAsync(organizationOwner.Organization.Id);
        var eventEntity = await ApiTestHelper.CreateEventAsync(organizationOwnerClient, ApiTestHelper.CreateEventRequest(organizationOwner.Organization.Id));

        var response = await organizationOwnerClient.PostAsJsonAsync($"/api/v1/events/{eventEntity.Id}/publish", new PublishEventRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EventDetailResponse>();
        body!.Status.Should().Be("Published");
    }

    [Fact]
    public async Task NonVerifiedOrganization_CannotPublishEvent()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("event-publish-pending-owner");
        using var organizationOwnerClient = organizationOwner.Client;
        var eventEntity = await ApiTestHelper.CreateEventAsync(organizationOwnerClient, ApiTestHelper.CreateEventRequest(organizationOwner.Organization.Id));

        var response = await organizationOwnerClient.PostAsJsonAsync($"/api/v1/events/{eventEntity.Id}/publish", new PublishEventRequest());

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("BUSINESS_RULE_VIOLATION");
    }

    [Fact]
    public async Task PublicEventsEndpoint_ReturnsOnlyPublishedEvents()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("event-public-owner");
        using var organizationOwnerClient = organizationOwner.Client;
        await VerifyOrganizationAsync(organizationOwner.Organization.Id);

        var publishedEvent = await ApiTestHelper.CreateEventAsync(organizationOwnerClient, ApiTestHelper.CreateEventRequest(organizationOwner.Organization.Id));
        await ApiTestHelper.PublishEventAsync(organizationOwnerClient, publishedEvent.Id);

        var draftEvent = await ApiTestHelper.CreateEventAsync(
            organizationOwnerClient,
            ApiTestHelper.CreateEventRequest(organizationOwner.Organization.Id, DateTimeOffset.UtcNow.AddDays(12)));

        using var anonymousClient = factory.CreateApiClient();
        var response = await anonymousClient.GetAsync("/api/v1/events?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<EventListItemResponse>>();
        body!.Items.Should().Contain(item => item.Id == publishedEvent.Id);
        body.Items.Should().NotContain(item => item.Id == draftEvent.Id);
    }

    [Fact]
    public async Task Owner_CanListOwnEvents()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("event-mine-owner");
        using var organizationOwnerClient = organizationOwner.Client;
        var eventEntity = await ApiTestHelper.CreateEventAsync(organizationOwnerClient, ApiTestHelper.CreateEventRequest(organizationOwner.Organization.Id));

        var response = await organizationOwnerClient.GetAsync("/api/v1/events/mine?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<EventListItemResponse>>();
        body!.Items.Should().Contain(item => item.Id == eventEntity.Id);
    }

    [Fact]
    public async Task Owner_CanUpdateDraftEvent()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("event-update-owner");
        using var organizationOwnerClient = organizationOwner.Client;
        var eventEntity = await ApiTestHelper.CreateEventAsync(organizationOwnerClient, ApiTestHelper.CreateEventRequest(organizationOwner.Organization.Id));

        var response = await organizationOwnerClient.PutAsJsonAsync(
            $"/api/v1/events/{eventEntity.Id}",
            new UpdateEventRequest
            {
                Title = "Updated Event Title",
                Description = "Updated description"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EventDetailResponse>();
        body!.Title.Should().Be("Updated Event Title");
        body.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task Owner_CanUploadEventImage()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("event-image-owner");
        using var organizationOwnerClient = organizationOwner.Client;
        var eventEntity = await ApiTestHelper.CreateEventAsync(organizationOwnerClient, ApiTestHelper.CreateEventRequest(organizationOwner.Organization.Id));

        using var content = ApiTestHelper.CreateImageUpload();
        var response = await organizationOwnerClient.PostAsync($"/api/v1/events/{eventEntity.Id}/image", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EventDetailResponse>();
        body!.ImagePath.Should().StartWith("/uploads/events/images/");
    }

    [Fact]
    public async Task Owner_CanCancelEvent()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("event-cancel-owner");
        using var organizationOwnerClient = organizationOwner.Client;
        var eventEntity = await ApiTestHelper.CreateEventAsync(organizationOwnerClient, ApiTestHelper.CreateEventRequest(organizationOwner.Organization.Id));

        var response = await organizationOwnerClient.PostAsJsonAsync($"/api/v1/events/{eventEntity.Id}/cancel", new CancelEventRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EventDetailResponse>();
        body!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task CancelledEvent_IsNotInPublicCatalog()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("event-cancelled-public-owner");
        using var organizationOwnerClient = organizationOwner.Client;
        await VerifyOrganizationAsync(organizationOwner.Organization.Id);

        var eventEntity = await ApiTestHelper.CreateEventAsync(organizationOwnerClient, ApiTestHelper.CreateEventRequest(organizationOwner.Organization.Id));
        await ApiTestHelper.PublishEventAsync(organizationOwnerClient, eventEntity.Id);
        await ApiTestHelper.CancelEventAsync(organizationOwnerClient, eventEntity.Id);

        using var anonymousClient = factory.CreateApiClient();
        var response = await anonymousClient.GetAsync("/api/v1/events?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<EventListItemResponse>>();
        body!.Items.Should().NotContain(item => item.Id == eventEntity.Id);
    }

    private async Task VerifyOrganizationAsync(Guid organizationId)
    {
        using var adminClient = factory.CreateApiClient();
        var adminLogin = await ApiTestHelper.LoginAsync(adminClient, factory.AdminEmail, factory.AdminPassword);
        PostgresWebApplicationFactory.SetBearerToken(adminClient, adminLogin.AccessToken);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/organizations/{organizationId}/verify",
            new VerifyOrganizationRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<(HttpClient Client, HairyPaws.Contracts.Identity.Responses.AuthResponse Login, OrganizationDetailResponse Organization)> CreateOngOrganizationClientAsync(string emailPrefix)
    {
        var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail(emailPrefix), "Ong");
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);
        var organization = await ApiTestHelper.CreateOrganizationAsync(client);
        return (client, login, organization);
    }
}
