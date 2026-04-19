using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Donations.Requests;
using HairyPaws.Contracts.Donations.Responses;
using HairyPaws.Contracts.Notifications.Responses;
using HairyPaws.Contracts.Organizations.Responses;
using HairyPaws.Tests.Integration.Common;

namespace HairyPaws.Tests.Integration.Donations;

public sealed class DonationsEndpointsTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AuthenticatedUser_CanCreateMoneyDonation()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("donation-money-org");
        using var organizationOwnerClient = organizationOwner.Client;

        var donor = await CreateAuthenticatedClientAsync("donation-money-donor", "Adopter");
        using var donorClient = donor.Client;

        var response = await donorClient.PostAsJsonAsync(
            "/api/v1/donations",
            ApiTestHelper.CreateMoneyDonationRequest(organizationOwner.Organization.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<DonationResponse>();
        body.Should().NotBeNull();
        body!.DonationType.Should().Be("Money");
        body.Status.Should().Be("Pending");
        body.Amount.Should().Be(50);
    }

    [Fact]
    public async Task AuthenticatedUser_CanCreateItemsDonation()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("donation-items-org");
        using var organizationOwnerClient = organizationOwner.Client;

        var donor = await CreateAuthenticatedClientAsync("donation-items-donor", "Adopter");
        using var donorClient = donor.Client;

        var response = await donorClient.PostAsJsonAsync(
            "/api/v1/donations",
            ApiTestHelper.CreateItemsDonationRequest(organizationOwner.Organization.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<DonationResponse>();
        body.Should().NotBeNull();
        body!.DonationType.Should().Be("Items");
        body.Items.Should().ContainSingle(item => item.Name == "Dog Food" && item.Quantity == 3);
    }

    [Fact]
    public async Task MoneyDonation_WithoutAmount_ShouldFail()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("donation-money-fail-org");
        using var organizationOwnerClient = organizationOwner.Client;

        var donor = await CreateAuthenticatedClientAsync("donation-money-fail-donor", "Adopter");
        using var donorClient = donor.Client;

        var response = await donorClient.PostAsJsonAsync(
            "/api/v1/donations",
            new CreateDonationRequest
            {
                OrganizationId = organizationOwner.Organization.Id,
                DonationType = "Money"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task ItemsDonation_WithoutItems_ShouldFail()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("donation-items-fail-org");
        using var organizationOwnerClient = organizationOwner.Client;

        var donor = await CreateAuthenticatedClientAsync("donation-items-fail-donor", "Adopter");
        using var donorClient = donor.Client;

        var response = await donorClient.PostAsJsonAsync(
            "/api/v1/donations",
            new CreateDonationRequest
            {
                OrganizationId = organizationOwner.Organization.Id,
                DonationType = "Items"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Donor_CanListOwnDonations()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("donation-list-org");
        using var organizationOwnerClient = organizationOwner.Client;

        var donor = await CreateAuthenticatedClientAsync("donation-list-donor", "Adopter");
        using var donorClient = donor.Client;

        await ApiTestHelper.CreateDonationAsync(donorClient, ApiTestHelper.CreateMoneyDonationRequest(organizationOwner.Organization.Id, 25));
        await ApiTestHelper.CreateDonationAsync(donorClient, ApiTestHelper.CreateItemsDonationRequest(organizationOwner.Organization.Id));

        var response = await donorClient.GetAsync("/api/v1/donations/my?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<DonationListItemResponse>>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCount(2);
        body.Items.Should().OnlyContain(item => item.Donor.Email == donor.Login.User.Email);
    }

    [Fact]
    public async Task OrganizationOwner_CanListDonationsReceivedByOwnOrganization()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("donation-org-list");
        using var organizationOwnerClient = organizationOwner.Client;

        var donor = await CreateAuthenticatedClientAsync("donation-org-list-donor", "Adopter");
        using var donorClient = donor.Client;
        var donation = await ApiTestHelper.CreateDonationAsync(donorClient, ApiTestHelper.CreateMoneyDonationRequest(organizationOwner.Organization.Id, 99));

        var response = await organizationOwnerClient.GetAsync("/api/v1/organizations/me/donations?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<DonationListItemResponse>>();
        body.Should().NotBeNull();
        body!.Items.Should().Contain(item => item.Id == donation.Id);
    }

    [Fact]
    public async Task UnauthorizedUser_CannotSeeAnotherDonation()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("donation-private-org");
        using var organizationOwnerClient = organizationOwner.Client;

        var donor = await CreateAuthenticatedClientAsync("donation-private-donor", "Adopter");
        using var donorClient = donor.Client;
        var donation = await ApiTestHelper.CreateDonationAsync(donorClient, ApiTestHelper.CreateMoneyDonationRequest(organizationOwner.Organization.Id));

        var otherUser = await CreateAuthenticatedClientAsync("donation-private-other", "Adopter");
        using var otherUserClient = otherUser.Client;

        var response = await otherUserClient.GetAsync($"/api/v1/donations/{donation.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OrganizationOwner_CanConfirmDonation()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("donation-confirm-owner");
        using var organizationOwnerClient = organizationOwner.Client;

        var donor = await CreateAuthenticatedClientAsync("donation-confirm-donor", "Adopter");
        using var donorClient = donor.Client;
        var donation = await ApiTestHelper.CreateDonationAsync(donorClient, ApiTestHelper.CreateMoneyDonationRequest(organizationOwner.Organization.Id));

        var response = await organizationOwnerClient.PostAsJsonAsync($"/api/v1/donations/{donation.Id}/confirm", new ConfirmDonationRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConfirmingDonation_ChangesStatusToConfirmed()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("donation-confirm-status-owner");
        using var organizationOwnerClient = organizationOwner.Client;

        var donor = await CreateAuthenticatedClientAsync("donation-confirm-status-donor", "Adopter");
        using var donorClient = donor.Client;
        var donation = await ApiTestHelper.CreateDonationAsync(donorClient, ApiTestHelper.CreateMoneyDonationRequest(organizationOwner.Organization.Id));

        await ApiTestHelper.ConfirmDonationAsync(organizationOwnerClient, donation.Id);

        var response = await donorClient.GetAsync($"/api/v1/donations/{donation.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DonationResponse>();
        body!.Status.Should().Be("Confirmed");
        body.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task OrganizationOwner_CanCancelDonation()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("donation-cancel-owner");
        using var organizationOwnerClient = organizationOwner.Client;

        var donor = await CreateAuthenticatedClientAsync("donation-cancel-donor", "Adopter");
        using var donorClient = donor.Client;
        var donation = await ApiTestHelper.CreateDonationAsync(donorClient, ApiTestHelper.CreateMoneyDonationRequest(organizationOwner.Organization.Id));

        var response = await organizationOwnerClient.PostAsJsonAsync($"/api/v1/donations/{donation.Id}/cancel", new CancelDonationRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DonationResponse>();
        body!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task ConfirmedDonation_CannotBeCancelled()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("donation-confirmed-cancel-owner");
        using var organizationOwnerClient = organizationOwner.Client;

        var donor = await CreateAuthenticatedClientAsync("donation-confirmed-cancel-donor", "Adopter");
        using var donorClient = donor.Client;
        var donation = await ApiTestHelper.CreateDonationAsync(donorClient, ApiTestHelper.CreateMoneyDonationRequest(organizationOwner.Organization.Id));
        await ApiTestHelper.ConfirmDonationAsync(organizationOwnerClient, donation.Id);

        var response = await organizationOwnerClient.PostAsJsonAsync($"/api/v1/donations/{donation.Id}/cancel", new CancelDonationRequest());

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("BUSINESS_RULE_VIOLATION");
    }

    [Fact]
    public async Task CancelledDonation_CannotBeConfirmed()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("donation-cancelled-confirm-owner");
        using var organizationOwnerClient = organizationOwner.Client;

        var donor = await CreateAuthenticatedClientAsync("donation-cancelled-confirm-donor", "Adopter");
        using var donorClient = donor.Client;
        var donation = await ApiTestHelper.CreateDonationAsync(donorClient, ApiTestHelper.CreateMoneyDonationRequest(organizationOwner.Organization.Id));
        await ApiTestHelper.CancelDonationAsync(organizationOwnerClient, donation.Id);

        var response = await organizationOwnerClient.PostAsJsonAsync($"/api/v1/donations/{donation.Id}/confirm", new ConfirmDonationRequest());

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("BUSINESS_RULE_VIOLATION");
    }

    [Fact]
    public async Task Donor_CanUploadReceiptWhilePending()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("donation-receipt-owner");
        using var organizationOwnerClient = organizationOwner.Client;

        var donor = await CreateAuthenticatedClientAsync("donation-receipt-donor", "Adopter");
        using var donorClient = donor.Client;
        var donation = await ApiTestHelper.CreateDonationAsync(donorClient, ApiTestHelper.CreateMoneyDonationRequest(organizationOwner.Organization.Id));

        using var content = ApiTestHelper.CreateReceiptUpload();
        var response = await donorClient.PostAsync($"/api/v1/donations/{donation.Id}/receipt", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DonationResponse>();
        body!.ReceiptPath.Should().StartWith("/uploads/donations/receipts/");
    }

    [Fact]
    public async Task Donor_CannotUploadReceiptAfterDonationIsConfirmed()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("donation-receipt-confirmed-owner");
        using var organizationOwnerClient = organizationOwner.Client;

        var donor = await CreateAuthenticatedClientAsync("donation-receipt-confirmed-donor", "Adopter");
        using var donorClient = donor.Client;
        var donation = await ApiTestHelper.CreateDonationAsync(donorClient, ApiTestHelper.CreateMoneyDonationRequest(organizationOwner.Organization.Id));
        await ApiTestHelper.ConfirmDonationAsync(organizationOwnerClient, donation.Id);

        using var content = ApiTestHelper.CreateReceiptUpload();
        var response = await donorClient.PostAsync($"/api/v1/donations/{donation.Id}/receipt", content);

        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task DonationCreation_CreatesNotificationForOrganizationOwner()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("donation-created-notification-owner");
        using var organizationOwnerClient = organizationOwner.Client;

        var donor = await CreateAuthenticatedClientAsync("donation-created-notification-donor", "Adopter");
        using var donorClient = donor.Client;
        var donation = await ApiTestHelper.CreateDonationAsync(donorClient, ApiTestHelper.CreateMoneyDonationRequest(organizationOwner.Organization.Id));

        var notificationsResponse = await organizationOwnerClient.GetAsync("/api/v1/notifications?page=1&pageSize=10");

        notificationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await notificationsResponse.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>();
        body!.Items.Should().Contain(notification =>
            notification.Type == "DonationCreated" &&
            notification.ReferenceId == donation.Id);
    }

    [Fact]
    public async Task ConfirmingDonation_CreatesNotificationForDonor()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("donation-confirm-notification-owner");
        using var organizationOwnerClient = organizationOwner.Client;

        var donor = await CreateAuthenticatedClientAsync("donation-confirm-notification-donor", "Adopter");
        using var donorClient = donor.Client;
        var donation = await ApiTestHelper.CreateDonationAsync(donorClient, ApiTestHelper.CreateMoneyDonationRequest(organizationOwner.Organization.Id));

        await ApiTestHelper.ConfirmDonationAsync(organizationOwnerClient, donation.Id);

        var notificationsResponse = await donorClient.GetAsync("/api/v1/notifications?page=1&pageSize=10");
        notificationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await notificationsResponse.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>();
        body!.Items.Should().Contain(notification =>
            notification.Type == "DonationConfirmed" &&
            notification.ReferenceId == donation.Id);
    }

    [Fact]
    public async Task CancellingDonation_CreatesNotificationForDonor()
    {
        var organizationOwner = await CreateOngOrganizationClientAsync("donation-cancel-notification-owner");
        using var organizationOwnerClient = organizationOwner.Client;

        var donor = await CreateAuthenticatedClientAsync("donation-cancel-notification-donor", "Adopter");
        using var donorClient = donor.Client;
        var donation = await ApiTestHelper.CreateDonationAsync(donorClient, ApiTestHelper.CreateMoneyDonationRequest(organizationOwner.Organization.Id));

        await ApiTestHelper.CancelDonationAsync(organizationOwnerClient, donation.Id);

        var notificationsResponse = await donorClient.GetAsync("/api/v1/notifications?page=1&pageSize=10");
        notificationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await notificationsResponse.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>();
        body!.Items.Should().Contain(notification =>
            notification.Type == "DonationCancelled" &&
            notification.ReferenceId == donation.Id);
    }

    private async Task<(HttpClient Client, HairyPaws.Contracts.Identity.Responses.AuthResponse Login)> CreateAuthenticatedClientAsync(string emailPrefix, string role)
    {
        var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail(emailPrefix), role);
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);
        return (client, login);
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
