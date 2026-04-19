using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Notifications.Responses;
using HairyPaws.Domain.Notifications.Entities;
using HairyPaws.Domain.Notifications.Enums;
using HairyPaws.Tests.Integration.Common;

namespace HairyPaws.Tests.Integration.Notifications;

public sealed class NotificationsEndpointsTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task User_CanListOwnNotifications()
    {
        var user = await CreateAuthenticatedClientAsync("notification-list", "Adopter");
        using var userClient = user.Client;
        await SeedNotificationAsync(user.Login.User.Id, NotificationType.Generic, "Hello", "First notification");

        var response = await userClient.GetAsync("/api/v1/notifications?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>();
        body!.Items.Should().ContainSingle(notification => notification.Title == "Hello");
    }

    [Fact]
    public async Task User_CannotReadAnotherUsersNotification()
    {
        var owner = await CreateAuthenticatedClientAsync("notification-owner", "Adopter");
        using var ownerClient = owner.Client;
        var intruder = await CreateAuthenticatedClientAsync("notification-intruder", "Adopter");
        using var intruderClient = intruder.Client;
        var notificationId = await SeedNotificationAsync(owner.Login.User.Id, NotificationType.Generic, "Private", "Owner notification");

        var response = await intruderClient.PostAsync($"/api/v1/notifications/{notificationId}/read", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task User_CanMarkOneNotificationAsRead()
    {
        var user = await CreateAuthenticatedClientAsync("notification-single-read", "Adopter");
        using var userClient = user.Client;
        var notificationId = await SeedNotificationAsync(user.Login.User.Id, NotificationType.Generic, "Read me", "Single notification");

        var response = await userClient.PostAsync($"/api/v1/notifications/{notificationId}/read", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<NotificationResponse>();
        body!.IsRead.Should().BeTrue();
        body.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task User_CanMarkAllNotificationsAsRead()
    {
        var user = await CreateAuthenticatedClientAsync("notification-read-all", "Adopter");
        using var userClient = user.Client;
        await SeedNotificationAsync(user.Login.User.Id, NotificationType.Generic, "One", "First");
        await SeedNotificationAsync(user.Login.User.Id, NotificationType.Generic, "Two", "Second");

        var response = await userClient.PostAsync("/api/v1/notifications/read-all", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await userClient.GetAsync("/api/v1/notifications?page=1&pageSize=10");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await listResponse.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>();
        body!.Items.Should().OnlyContain(notification => notification.IsRead);
    }

    [Fact]
    public async Task UnreadCountEndpoint_ReturnsCorrectCount()
    {
        var user = await CreateAuthenticatedClientAsync("notification-count", "Adopter");
        using var userClient = user.Client;
        var readNotificationId = await SeedNotificationAsync(user.Login.User.Id, NotificationType.Generic, "Read", "Already read");
        await SeedNotificationAsync(user.Login.User.Id, NotificationType.Generic, "Unread one", "Unread message one");
        await SeedNotificationAsync(user.Login.User.Id, NotificationType.Generic, "Unread two", "Unread message two");

        await userClient.PostAsync($"/api/v1/notifications/{readNotificationId}/read", content: null);

        var response = await userClient.GetAsync("/api/v1/notifications/unread-count");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UnreadNotificationsCountResponse>();
        body!.Count.Should().Be(2);
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

    private async Task<(HttpClient Client, HairyPaws.Contracts.Identity.Responses.AuthResponse Login)> CreateAuthenticatedClientAsync(string emailPrefix, string role)
    {
        var client = factory.CreateApiClient();
        var login = await ApiTestHelper.RegisterAndLoginAsync(client, ApiTestHelper.UniqueEmail(emailPrefix), role);
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);
        return (client, login);
    }
}
