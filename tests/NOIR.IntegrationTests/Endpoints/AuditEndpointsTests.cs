using NOIR.Application.Common.Models;
using NOIR.Application.Features.Audit.DTOs;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for audit/activity timeline endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class AuditEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuditEndpointsTests(CustomWebApplicationFactory factory)
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
            RoleNames: null); // No roles - regular user without admin permissions

        var createResponse = await adminClient.PostAsJsonAsync("/api/users", createCommand);
        createResponse.EnsureSuccessStatusCode();

        // Login as the created user
        var loginCommand = new LoginCommand(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        return (email, password, auth!);
    }

    #region Search Activity Timeline Tests

    [Fact]
    public async Task SearchActivityTimeline_AsAdmin_ShouldReturnPagedResult()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/audit/activity-timeline?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ActivityTimelineEntryDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchActivityTimeline_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/audit/activity-timeline?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SearchActivityTimeline_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without audit permissions
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync("/api/audit/activity-timeline?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SearchActivityTimeline_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/audit/activity-timeline?page=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ActivityTimelineEntryDto>>();
        result.Should().NotBeNull();
        result!.PageNumber.Should().Be(1);
        result.Items.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task SearchActivityTimeline_WithOperationTypeFilter_ShouldFilterResults()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act - filter by Create operations
        var response = await adminClient.GetAsync("/api/audit/activity-timeline?page=1&pageSize=10&operationType=Create");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ActivityTimelineEntryDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().OnlyContain(e => e.OperationType == "Create");
    }

    [Fact]
    public async Task SearchActivityTimeline_WithDateFilter_ShouldFilterResults()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7).ToString("o");
        var toDate = DateTimeOffset.UtcNow.AddDays(1).ToString("o");

        // Act
        var response = await adminClient.GetAsync(
            $"/api/audit/activity-timeline?page=1&pageSize=10&fromDate={Uri.EscapeDataString(fromDate)}&toDate={Uri.EscapeDataString(toDate)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ActivityTimelineEntryDto>>();
        result.Should().NotBeNull();
    }

    #endregion

    #region Get Activity Details Tests

    [Fact]
    public async Task GetActivityDetails_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/audit/activity-timeline/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetActivityDetails_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/audit/activity-timeline/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetActivityDetails_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without audit permissions
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync($"/api/audit/activity-timeline/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Get Page Contexts Tests

    [Fact]
    public async Task GetPageContexts_AsAdmin_ShouldReturnList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/audit/page-contexts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<string>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPageContexts_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/audit/page-contexts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPageContexts_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without audit permissions
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync("/api/audit/page-contexts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}
