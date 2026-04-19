using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Events.Requests;
using HairyPaws.Contracts.Identity.Requests;
using HairyPaws.Contracts.Identity.Responses;
using HairyPaws.Contracts.Notifications.Responses;
using HairyPaws.Contracts.Organizations.Requests;
using HairyPaws.Contracts.Organizations.Responses;
using HairyPaws.Contracts.Pets.Responses;
using HairyPaws.Domain.Audit.Entities;
using HairyPaws.Domain.Notifications.Entities;
using HairyPaws.Domain.Notifications.Enums;
using HairyPaws.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Tests.Integration.Hardening;

public sealed class HardeningEndpointsTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SensitiveOrganizationAction_CreatesAuditLog()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("audit-org-create");
        using var _ = organizationOwner.Client;
        var organization = organizationOwner.Organization;

        var auditLog = await GetAuditLogAsync("Organization", organization.Id, "Create");

        auditLog.Should().NotBeNull();
        auditLog!.PerformedByUserId.Should().Be(organizationOwner.Login.User.Id);
        auditLog.AfterJson.Should().Contain("\"name\":\"Happy Tails ONG\"");
    }

    [Fact]
    public async Task PetPublish_CreatesAuditLog()
    {
        var owner = await CreateAuthenticatedClientAsync("audit-pet-owner", "Owner");
        using var ownerClient = owner.Client;

        var pet = await ApiTestHelper.CreatePetAsync(ownerClient);
        await ApiTestHelper.UploadPhotoAsync(ownerClient, pet.Id);
        await ApiTestHelper.PublishPetAsync(ownerClient, pet.Id);

        var auditLog = await GetAuditLogAsync("Pet", pet.Id, "Publish");

        auditLog.Should().NotBeNull();
        auditLog!.BeforeJson.Should().Contain("\"status\":\"Draft\"");
        auditLog.AfterJson.Should().Contain("\"status\":\"Available\"");
    }

    [Fact]
    public async Task AdoptionApprove_CreatesAuditLog()
    {
        var owner = await CreateAuthenticatedClientAsync("audit-adoption-owner", "Owner");
        using var ownerClient = owner.Client;
        var adopter = await CreateAuthenticatedClientAsync("audit-adoption-adopter", "Adopter");
        using var adopterClient = adopter.Client;

        var pet = await ApiTestHelper.CreateAvailablePetAsync(ownerClient);
        var adoptionRequest = await ApiTestHelper.SubmitAdoptionRequestAsync(adopterClient, pet.Id);
        await ApiTestHelper.ApproveAdoptionRequestAsync(ownerClient, adoptionRequest.Id, "Approved after review");

        var auditLog = await GetAuditLogAsync("AdoptionRequest", adoptionRequest.Id, "Approve");

        auditLog.Should().NotBeNull();
        auditLog!.AfterJson.Should().Contain("\"status\":\"Approved\"");
        auditLog.MetadataJson.Should().Contain("PendingAdoption");
    }

    [Fact]
    public async Task DonationConfirm_CreatesAuditLog()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("audit-donation-owner");
        using var organizationOwnerClient = organizationOwner.Client;
        var donor = await CreateAuthenticatedClientAsync("audit-donation-donor", "Adopter");
        using var donorClient = donor.Client;

        var donation = await ApiTestHelper.CreateDonationAsync(
            donorClient,
            ApiTestHelper.CreateMoneyDonationRequest(organizationOwner.Organization.Id));

        await ApiTestHelper.ConfirmDonationAsync(organizationOwnerClient, donation.Id);

        var auditLog = await GetAuditLogAsync("Donation", donation.Id, "Confirm");

        auditLog.Should().NotBeNull();
        auditLog!.AfterJson.Should().Contain("\"status\":\"Confirmed\"");
    }

    [Fact]
    public async Task EventPublish_CreatesAuditLog()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("audit-event-owner");
        using var organizationOwnerClient = organizationOwner.Client;
        await VerifyOrganizationAsync(organizationOwner.Organization.Id);

        var eventEntity = await ApiTestHelper.CreateEventAsync(
            organizationOwnerClient,
            ApiTestHelper.CreateEventRequest(organizationOwner.Organization.Id));

        await ApiTestHelper.PublishEventAsync(organizationOwnerClient, eventEntity.Id);

        var auditLog = await GetAuditLogAsync("Event", eventEntity.Id, "Publish");

        auditLog.Should().NotBeNull();
        auditLog!.AfterJson.Should().Contain("\"status\":\"Published\"");
    }

    [Fact]
    public async Task AdminResetPassword_CreatesAuditLog()
    {
        using var userClient = factory.CreateApiClient();
        var userLogin = await ApiTestHelper.RegisterAndLoginAsync(userClient, ApiTestHelper.UniqueEmail("audit-password-user"), "Adopter");

        using var adminClient = factory.CreateApiClient();
        var adminLogin = await ApiTestHelper.LoginAsync(adminClient, factory.AdminEmail, factory.AdminPassword);
        PostgresWebApplicationFactory.SetBearerToken(adminClient, adminLogin.AccessToken);

        var response = await adminClient.PostAsJsonAsync(
            "/api/v1/auth/admin-reset-password",
            new AdminResetPasswordRequest
            {
                UserId = userLogin.User.Id,
                NewPassword = "ResetPassword123!"
            });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var auditLog = await GetAuditLogAsync("User", userLogin.User.Id, "ResetPassword");

        auditLog.Should().NotBeNull();
        auditLog!.PerformedByUserId.Should().Be(adminLogin.User.Id);
        auditLog.MetadataJson.Should().Contain(userLogin.User.Email);
    }

    [Fact]
    public async Task HiddenNonPublicPet_IsNotReturnedInPublicCatalog()
    {
        var owner = await CreateAuthenticatedClientAsync("hidden-pet-owner", "Owner");
        using var ownerClient = owner.Client;

        var availablePet = await ApiTestHelper.CreateAvailablePetAsync(ownerClient);
        await ApiTestHelper.CreatePetAsync(ownerClient);

        using var anonymousClient = factory.CreateApiClient();
        var response = await anonymousClient.GetAsync("/api/v1/pets?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<HairyPaws.Contracts.Pets.Responses.PetListItemResponse>>();
        body!.Items.Should().Contain(item => item.Id == availablePet.Id);
        body.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task HiddenNonPublishedEvent_IsNotReturnedInPublicCatalog()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("hidden-event-owner");
        using var organizationOwnerClient = organizationOwner.Client;
        await VerifyOrganizationAsync(organizationOwner.Organization.Id);

        var publishedEvent = await ApiTestHelper.CreateEventAsync(
            organizationOwnerClient,
            ApiTestHelper.CreateEventRequest(organizationOwner.Organization.Id));
        await ApiTestHelper.PublishEventAsync(organizationOwnerClient, publishedEvent.Id);

        await ApiTestHelper.CreateEventAsync(
            organizationOwnerClient,
            ApiTestHelper.CreateEventRequest(organizationOwner.Organization.Id, DateTimeOffset.UtcNow.AddDays(15)));

        using var anonymousClient = factory.CreateApiClient();
        var response = await anonymousClient.GetAsync("/api/v1/events?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<HairyPaws.Contracts.Events.Responses.EventListItemResponse>>();
        body!.Items.Should().ContainSingle(item => item.Id == publishedEvent.Id);
    }

    [Fact]
    public async Task SoftDeletedEntities_AreNotExposedPublicly()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("soft-org-owner");
        using var organizationOwnerClient = organizationOwner.Client;
        await VerifyOrganizationAsync(organizationOwner.Organization.Id);

        var publishedEvent = await ApiTestHelper.CreateEventAsync(
            organizationOwnerClient,
            ApiTestHelper.CreateEventRequest(organizationOwner.Organization.Id));
        await ApiTestHelper.PublishEventAsync(organizationOwnerClient, publishedEvent.Id);

        var petOwner = await CreateAuthenticatedClientAsync("soft-pet-owner", "Owner");
        using var petOwnerClient = petOwner.Client;
        var availablePet = await ApiTestHelper.CreateAvailablePetAsync(petOwnerClient);

        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            var utcNow = DateTimeOffset.UtcNow;

            var organization = await dbContext.Organizations.SingleAsync(entity => entity.Id == organizationOwner.Organization.Id);
            organization.DeletedAt = utcNow;

            var pet = await dbContext.Pets.SingleAsync(entity => entity.Id == availablePet.Id);
            pet.DeletedAt = utcNow;

            var eventEntity = await dbContext.Events.SingleAsync(entity => entity.Id == publishedEvent.Id);
            eventEntity.DeletedAt = utcNow;
        });

        using var anonymousClient = factory.CreateApiClient();

        (await anonymousClient.GetAsync($"/api/v1/organizations/{organizationOwner.Organization.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await anonymousClient.GetAsync($"/api/v1/pets/{availablePet.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await anonymousClient.GetAsync($"/api/v1/events/{publishedEvent.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnauthorizedUser_CannotInspectPrivateOrganizationDocumentMetadata()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("private-org-owner");
        using var organizationOwnerClient = organizationOwner.Client;
        var organization = organizationOwner.Organization;
        using var content = ApiTestHelper.CreateDocumentUpload("License");
        var uploadResponse = await organizationOwnerClient.PostAsync($"/api/v1/organizations/{organization.Id}/documents", content);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var otherUser = await CreateAuthenticatedClientAsync("private-org-other", "Adopter");
        using var otherUserClient = otherUser.Client;

        var response = await otherUserClient.GetAsync($"/api/v1/organizations/{organization.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnauthorizedUser_CannotAccessAnotherAdoptersRequestDetails()
    {
        var owner = await CreateAuthenticatedClientAsync("private-request-owner", "Owner");
        using var ownerClient = owner.Client;
        var firstAdopter = await CreateAuthenticatedClientAsync("private-request-adopter", "Adopter");
        using var firstAdopterClient = firstAdopter.Client;
        var secondAdopter = await CreateAuthenticatedClientAsync("private-request-intruder", "Adopter");
        using var secondAdopterClient = secondAdopter.Client;

        var pet = await ApiTestHelper.CreateAvailablePetAsync(ownerClient);
        var adoptionRequest = await ApiTestHelper.SubmitAdoptionRequestAsync(firstAdopterClient, pet.Id);

        var response = await secondAdopterClient.GetAsync($"/api/v1/adoption-requests/{adoptionRequest.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnauthorizedUser_CannotAccessAnotherDonorsDonationDetails()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("private-donation-owner");
        using var organizationOwnerClient = organizationOwner.Client;
        var firstDonor = await CreateAuthenticatedClientAsync("private-donation-first", "Adopter");
        using var firstDonorClient = firstDonor.Client;
        var secondDonor = await CreateAuthenticatedClientAsync("private-donation-second", "Adopter");
        using var secondDonorClient = secondDonor.Client;

        var donation = await ApiTestHelper.CreateDonationAsync(
            firstDonorClient,
            ApiTestHelper.CreateMoneyDonationRequest(organizationOwner.Organization.Id));

        var response = await secondDonorClient.GetAsync($"/api/v1/donations/{donation.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task NotificationUnreadCount_RemainsCorrect_AfterMarkOneRead()
    {
        var user = await CreateAuthenticatedClientAsync("hardening-notification-one", "Adopter");
        using var userClient = user.Client;

        var firstNotificationId = await SeedNotificationAsync(user.Login.User.Id, NotificationType.Generic, "One", "First");
        await SeedNotificationAsync(user.Login.User.Id, NotificationType.Generic, "Two", "Second");

        var readResponse = await userClient.PostAsync($"/api/v1/notifications/{firstNotificationId}/read", content: null);
        readResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var count = await ApiTestHelper.GetUnreadNotificationsCountAsync(userClient);
        count.Count.Should().Be(1);
    }

    [Fact]
    public async Task NotificationUnreadCount_BecomesZero_AfterMarkAllRead()
    {
        var user = await CreateAuthenticatedClientAsync("hardening-notification-all", "Adopter");
        using var userClient = user.Client;

        await SeedNotificationAsync(user.Login.User.Id, NotificationType.Generic, "One", "First");
        await SeedNotificationAsync(user.Login.User.Id, NotificationType.Generic, "Two", "Second");

        var readAllResponse = await userClient.PostAsync("/api/v1/notifications/read-all", content: null);
        readAllResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var count = await ApiTestHelper.GetUnreadNotificationsCountAsync(userClient);
        count.Count.Should().Be(0);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        using var client = factory.CreateApiClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task ReadyEndpoint_ReturnsOk_WhenDatabaseIsReachable()
    {
        using var client = factory.CreateApiClient();

        var response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
        document.RootElement.GetProperty("checks").TryGetProperty("database", out _).Should().BeTrue();
    }

    private async Task<AuditLog?> GetAuditLogAsync(string entityName, Guid entityId, string action)
    {
        return await factory.ExecuteDbContextAsync(dbContext =>
            dbContext.AuditLogs
                .AsNoTracking()
                .OrderByDescending(entity => entity.CreatedAt)
                .SingleOrDefaultAsync(entity =>
                    entity.EntityName == entityName &&
                    entity.EntityId == entityId &&
                    entity.Action == action));
    }

    private async Task<Guid> SeedNotificationAsync(Guid userId, NotificationType type, string title, string message)
    {
        return await factory.ExecuteDbContextAsync(async dbContext =>
        {
            var notification = Notification.Create(userId, type, title, message, null, null, DateTimeOffset.UtcNow);
            await dbContext.Notifications.AddAsync(notification);
            return notification.Id;
        });
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

    private async Task<(HttpClient Client, AuthResponse Login)> CreateAuthenticatedClientAsync(string emailPrefix, string role)
    {
        var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail(emailPrefix), role);
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);
        return (client, login);
    }

    private async Task<(HttpClient Client, AuthResponse Login, OrganizationDetailResponse Organization)> CreateOngOrganizationClientAsync(string emailPrefix)
    {
        var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail(emailPrefix), "Ong");
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);
        var organization = await ApiTestHelper.CreateOrganizationAsync(client);
        return (client, login, organization);
    }
}
