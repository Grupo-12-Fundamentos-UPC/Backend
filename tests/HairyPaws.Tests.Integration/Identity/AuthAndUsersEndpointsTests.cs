using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HairyPaws.Contracts.Common.Requests;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Identity.Requests;
using HairyPaws.Contracts.Identity.Responses;
using HairyPaws.Contracts.Users.Requests;
using HairyPaws.Contracts.Users.Responses;
using HairyPaws.Tests.Integration.Common;

namespace HairyPaws.Tests.Integration.Identity;

public sealed class AuthAndUsersEndpointsTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_ShouldSucceed()
    {
        using var client = factory.CreateApiClient();
        var request = CreateRegisterRequest("adopter1@hairypaws.test");

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<UserSummaryResponse>();
        body.Should().NotBeNull();
        body!.Email.Should().Be(request.Email.ToLowerInvariant());
        body.Role.Should().Be(request.Role);
    }

    [Fact]
    public async Task Register_ShouldFail_WhenEmailAlreadyExists()
    {
        using var client = factory.CreateApiClient();
        var request = CreateRegisterRequest("duplicate@hairypaws.test");

        await client.PostAsJsonAsync("/api/v1/auth/register", request);
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("CONFLICT");
    }

    [Fact]
    public async Task Login_ShouldSucceed()
    {
        using var client = factory.CreateApiClient();
        var request = CreateRegisterRequest("login-success@hairypaws.test");
        await client.PostAsJsonAsync("/api/v1/auth/register", request);

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest { Email = request.Email, Password = request.Password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.User.Email.Should().Be(request.Email.ToLowerInvariant());
    }

    [Fact]
    public async Task Login_ShouldFail_WhenPasswordIsWrong()
    {
        using var client = factory.CreateApiClient();
        var request = CreateRegisterRequest("wrong-password@hairypaws.test");
        await client.PostAsJsonAsync("/api/v1/auth/register", request);

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest { Email = request.Email, Password = "WrongPassword123!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("UNAUTHORIZED");
    }

    [Fact]
    public async Task Refresh_ShouldRotateRefreshToken()
    {
        using var client = factory.CreateApiClient();
        var registerRequest = CreateRegisterRequest("refresh@hairypaws.test");
        await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var loginResponse = await LoginAsync(client, registerRequest.Email, registerRequest.Password);

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest { RefreshToken = loginResponse.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.RefreshToken.Should().NotBe(loginResponse.RefreshToken);
        body.AccessToken.Should().NotBe(loginResponse.AccessToken);
    }

    [Fact]
    public async Task ChangePassword_ShouldSucceed()
    {
        using var client = factory.CreateApiClient();
        var request = CreateRegisterRequest("change-password@hairypaws.test");
        await client.PostAsJsonAsync("/api/v1/auth/register", request);

        var login = await LoginAsync(client, request.Email, request.Password);
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);

        var changePasswordResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/change-password",
            new ChangePasswordRequest
            {
                CurrentPassword = request.Password,
                NewPassword = "NewPassword123!"
            });

        changePasswordResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var newLogin = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest { Email = request.Email, Password = "NewPassword123!" });

        newLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthMe_ShouldReturnAuthenticatedUser()
    {
        using var client = factory.CreateApiClient();
        var request = CreateRegisterRequest("auth-me@hairypaws.test");
        await client.PostAsJsonAsync("/api/v1/auth/register", request);

        var login = await LoginAsync(client, request.Email, request.Password);
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserSummaryResponse>();
        body!.Email.Should().Be(request.Email.ToLowerInvariant());
    }

    [Fact]
    public async Task Admin_ShouldListUsers()
    {
        using var client = factory.CreateApiClient();
        await client.PostAsJsonAsync("/api/v1/auth/register", CreateRegisterRequest("list-users@hairypaws.test"));

        var adminLogin = await LoginAsync(client, factory.AdminEmail, factory.AdminPassword);
        PostgresWebApplicationFactory.SetBearerToken(client, adminLogin.AccessToken);

        var response = await client.GetAsync("/api/v1/admin/users?pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<UserSummaryResponse>>();
        body.Should().NotBeNull();
        body!.Items.Should().Contain(item => item.Email == "list-users@hairypaws.test");
    }

    [Fact]
    public async Task NonAdmin_ShouldNotListUsers()
    {
        using var client = factory.CreateApiClient();
        var request = CreateRegisterRequest("non-admin@hairypaws.test");
        await client.PostAsJsonAsync("/api/v1/auth/register", request);
        var login = await LoginAsync(client, request.Email, request.Password);
        PostgresWebApplicationFactory.SetBearerToken(client, login.AccessToken);

        var response = await client.GetAsync("/api/v1/admin/users?pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_ShouldUpdateUserStatus()
    {
        using var client = factory.CreateApiClient();
        var registerRequest = CreateRegisterRequest("status@hairypaws.test");
        await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var userLogin = await LoginAsync(client, registerRequest.Email, registerRequest.Password);

        var adminLogin = await LoginAsync(client, factory.AdminEmail, factory.AdminPassword);
        PostgresWebApplicationFactory.SetBearerToken(client, adminLogin.AccessToken);

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/admin/users/{userLogin.User.Id}/status",
            new UpdateUserStatusRequest { Status = "Suspended" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserSummaryResponse>();
        body!.Status.Should().Be("Suspended");
    }

    [Fact]
    public async Task Admin_ShouldUpdateUserVerificationStatus()
    {
        using var client = factory.CreateApiClient();
        var registerRequest = CreateRegisterRequest("verification@hairypaws.test");
        await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var userLogin = await LoginAsync(client, registerRequest.Email, registerRequest.Password);

        var adminLogin = await LoginAsync(client, factory.AdminEmail, factory.AdminPassword);
        PostgresWebApplicationFactory.SetBearerToken(client, adminLogin.AccessToken);

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/admin/users/{userLogin.User.Id}/verify",
            new UpdateUserVerificationRequest { VerificationStatus = "Verified" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserSummaryResponse>();
        body!.VerificationStatus.Should().Be("Verified");
    }

    private static RegisterRequest CreateRegisterRequest(string email)
    {
        return new RegisterRequest
        {
            Email = email,
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
            Role = "Adopter",
            PhoneNumber = "5551234",
            Address = "Test Address"
        };
    }

    private static async Task<AuthResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest { Email = email, Password = password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        return body!;
    }
}
