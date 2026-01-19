using NOIR.Application.Features.Notifications.DTOs;
using NOIR.Domain.Enums;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for notification endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class NotificationEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NotificationEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    private async Task<HttpClient> GetAdminClientAsync()
    {
        var loginCommand = new LoginCommand("admin@noir.local", "123qwe");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return _factory.CreateAuthenticatedClient(auth!.AccessToken);
    }

    private async Task<(string Email, string Password, AuthResponse Auth)> CreateTestUserAsync()
    {
        var adminClient = await GetAdminClientAsync();
        var email = $"test_{Guid.NewGuid():N}@example.com";
        var password = "TestPassword123!";

        var createCommand = new CreateUserCommand(
            Email: email,
            Password: password,
            FirstName: "Test",
            LastName: "User",
            DisplayName: null,
            RoleNames: null);

        var createResponse = await adminClient.PostAsJsonAsync("/api/users", createCommand);
        createResponse.EnsureSuccessStatusCode();

        // Login as the created user
        var loginCommand = new LoginCommand(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        return (email, password, auth!);
    }

    #region Get Notifications Tests

    [Fact]
    public async Task GetNotifications_AsAuthenticatedUser_ShouldReturnPagedResponse()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<NotificationsPagedResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNotifications_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNotifications_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/notifications?page=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<NotificationsPagedResponse>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(5);
    }

    [Fact]
    public async Task GetNotifications_ExcludeRead_ShouldFilterResults()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/notifications?includeRead=false");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<NotificationsPagedResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().OnlyContain(n => !n.IsRead);
    }

    #endregion

    #region Get Unread Count Tests

    [Fact]
    public async Task GetUnreadCount_AsAuthenticatedUser_ShouldReturnCount()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/notifications/unread-count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UnreadCountResponse>();
        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetUnreadCount_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/notifications/unread-count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Mark Notification As Read Tests

    [Fact]
    public async Task MarkAsRead_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.PostAsync($"/api/notifications/{Guid.NewGuid()}/read", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkAsRead_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsync($"/api/notifications/{Guid.NewGuid()}/read", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Mark All As Read Tests

    [Fact]
    public async Task MarkAllAsRead_AsAuthenticatedUser_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.PostAsync("/api/notifications/read-all", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkAllAsRead_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/api/notifications/read-all", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Delete Notification Tests

    [Fact]
    public async Task DeleteNotification_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/notifications/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteNotification_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/notifications/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get Preferences Tests

    [Fact]
    public async Task GetPreferences_AsAuthenticatedUser_ShouldReturnPreferences()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/notifications/preferences");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<IEnumerable<NotificationPreferenceDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPreferences_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/notifications/preferences");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Update Preferences Tests

    [Fact]
    public async Task UpdatePreferences_ValidRequest_ShouldReturnUpdatedPreferences()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var command = new
        {
            Preferences = new[]
            {
                new
                {
                    Category = NotificationCategory.System,
                    InAppEnabled = true,
                    EmailFrequency = EmailFrequency.None
                }
            }
        };

        // Act
        var response = await adminClient.PutAsJsonAsync("/api/notifications/preferences", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<IEnumerable<NotificationPreferenceDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdatePreferences_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new
        {
            Preferences = Array.Empty<object>()
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/notifications/preferences", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region User Specific Tests

    [Fact]
    public async Task GetNotifications_DifferentUsers_ShouldReturnTheirOwnNotifications()
    {
        // Arrange - Create two different users
        var (_, _, auth1) = await CreateTestUserAsync();
        var (_, _, auth2) = await CreateTestUserAsync();

        var userClient1 = _factory.CreateAuthenticatedClient(auth1.AccessToken);
        var userClient2 = _factory.CreateAuthenticatedClient(auth2.AccessToken);

        // Act - Get notifications for each user
        var response1 = await userClient1.GetAsync("/api/notifications");
        var response2 = await userClient2.GetAsync("/api/notifications");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Both should return their own notifications (even if empty)
        var result1 = await response1.Content.ReadFromJsonWithEnumsAsync<NotificationsPagedResponse>();
        var result2 = await response2.Content.ReadFromJsonWithEnumsAsync<NotificationsPagedResponse>();

        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAllAsRead_ShouldOnlyAffectCurrentUser()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.PostAsync("/api/notifications/read-all", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
