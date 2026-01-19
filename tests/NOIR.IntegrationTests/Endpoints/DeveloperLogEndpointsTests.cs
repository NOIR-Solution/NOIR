using NOIR.Application.Features.DeveloperLogs.DTOs;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for developer log endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// Note: These endpoints require SystemAdmin permission.
/// </summary>
[Collection("Integration")]
public class DeveloperLogEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DeveloperLogEndpointsTests(CustomWebApplicationFactory factory)
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

    #region Get Log Level Tests

    [Fact]
    public async Task GetLogLevel_AsAdmin_ShouldReturnLogLevel()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/admin/developer-logs/level");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LogLevelResponse>();
        result.Should().NotBeNull();
        result!.Level.Should().NotBeNullOrEmpty();
        result.AvailableLevels.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLogLevel_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/developer-logs/level");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLogLevel_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without system admin permission
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync("/api/admin/developer-logs/level");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Set Log Level Tests

    [Fact]
    public async Task SetLogLevel_ValidLevel_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new ChangeLogLevelRequest("Information");

        // Act
        var response = await adminClient.PutAsJsonAsync("/api/admin/developer-logs/level", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LogLevelResponse>();
        result.Should().NotBeNull();
        result!.Level.Should().Be("Information");
    }

    [Fact]
    public async Task SetLogLevel_InvalidLevel_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new ChangeLogLevelRequest("InvalidLevel");

        // Act
        var response = await adminClient.PutAsJsonAsync("/api/admin/developer-logs/level", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetLogLevel_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new ChangeLogLevelRequest("Information");

        // Act
        var response = await _client.PutAsJsonAsync("/api/admin/developer-logs/level", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetLogLevel_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without system admin permission
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);
        var request = new ChangeLogLevelRequest("Information");

        // Act
        var response = await userClient.PutAsJsonAsync("/api/admin/developer-logs/level", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Get Log Level Overrides Tests

    [Fact]
    public async Task GetLogLevelOverrides_AsAdmin_ShouldReturnOverrides()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/admin/developer-logs/level/overrides");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LogLevelOverridesResponse>();
        result.Should().NotBeNull();
        result!.GlobalLevel.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetLogLevelOverrides_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/developer-logs/level/overrides");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Set Log Level Override Tests

    [Fact]
    public async Task SetLogLevelOverride_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var sourcePrefix = "Microsoft.AspNetCore";
        var request = new ChangeLogLevelRequest("Warning");

        // Act
        var response = await adminClient.PutAsJsonAsync(
            $"/api/admin/developer-logs/level/overrides/{Uri.EscapeDataString(sourcePrefix)}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetLogLevelOverride_InvalidLevel_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var sourcePrefix = "Microsoft.AspNetCore";
        var request = new ChangeLogLevelRequest("InvalidLevel");

        // Act
        var response = await adminClient.PutAsJsonAsync(
            $"/api/admin/developer-logs/level/overrides/{Uri.EscapeDataString(sourcePrefix)}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetLogLevelOverride_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new ChangeLogLevelRequest("Warning");

        // Act
        var response = await _client.PutAsJsonAsync(
            "/api/admin/developer-logs/level/overrides/Microsoft.AspNetCore",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Remove Log Level Override Tests

    [Fact]
    public async Task RemoveLogLevelOverride_NonExistent_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var sourcePrefix = "NonExistent.Namespace.That.Does.Not.Exist";

        // Act
        var response = await adminClient.DeleteAsync(
            $"/api/admin/developer-logs/level/overrides/{Uri.EscapeDataString(sourcePrefix)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveLogLevelOverride_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.DeleteAsync(
            "/api/admin/developer-logs/level/overrides/Microsoft.AspNetCore");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get Buffer Stats Tests

    [Fact]
    public async Task GetBufferStats_AsAdmin_ShouldReturnStats()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/admin/developer-logs/buffer/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LogBufferStatsDto>();
        result.Should().NotBeNull();
        result!.MaxCapacity.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetBufferStats_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/developer-logs/buffer/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBufferStats_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without system admin permission
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync("/api/admin/developer-logs/buffer/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Get Buffer Entries Tests

    [Fact]
    public async Task GetBufferEntries_AsAdmin_ShouldReturnEntries()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/admin/developer-logs/buffer/entries?count=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<IEnumerable<LogEntryDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBufferEntries_WithFilters_ShouldReturnFilteredEntries()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync(
            "/api/admin/developer-logs/buffer/entries?count=50&minLevel=Warning");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<IEnumerable<LogEntryDto>>();
        result.Should().NotBeNull();
        result.Should().OnlyContain(e => e.Level >= DevLogLevel.Warning);
    }

    [Fact]
    public async Task GetBufferEntries_WithSearch_ShouldFilterBySearch()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync(
            "/api/admin/developer-logs/buffer/entries?count=50&search=error");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBufferEntries_ExceptionsOnly_ShouldReturnOnlyExceptions()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync(
            "/api/admin/developer-logs/buffer/entries?count=50&exceptionsOnly=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<IEnumerable<LogEntryDto>>();
        result.Should().NotBeNull();
        result.Should().OnlyContain(e => e.Exception != null);
    }

    [Fact]
    public async Task GetBufferEntries_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/developer-logs/buffer/entries?count=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get Error Clusters Tests

    [Fact]
    public async Task GetErrorClusters_AsAdmin_ShouldReturnClusters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/admin/developer-logs/buffer/errors?maxClusters=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<IEnumerable<ErrorClusterDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetErrorClusters_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/developer-logs/buffer/errors?maxClusters=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Clear Buffer Tests

    [Fact]
    public async Task ClearBuffer_AsAdmin_ShouldReturnNoContent()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync("/api/admin/developer-logs/buffer");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ClearBuffer_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.DeleteAsync("/api/admin/developer-logs/buffer");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ClearBuffer_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without system admin permission
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.DeleteAsync("/api/admin/developer-logs/buffer");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Get Available Log Dates Tests

    [Fact]
    public async Task GetAvailableLogDates_AsAdmin_ShouldReturnDates()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/admin/developer-logs/history/dates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<string>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAvailableLogDates_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/developer-logs/history/dates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get Historical Logs Tests

    [Fact]
    public async Task GetHistoricalLogs_ValidDate_ShouldReturnLogs()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var today = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");

        // Act
        var response = await adminClient.GetAsync(
            $"/api/admin/developer-logs/history/{today}?page=1&pageSize=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<LogEntriesPagedResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetHistoricalLogs_InvalidDate_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync(
            "/api/admin/developer-logs/history/invalid-date?page=1&pageSize=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetHistoricalLogs_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync(
            "/api/admin/developer-logs/history/2024-01-01?page=1&pageSize=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Search Historical Logs Tests

    [Fact]
    public async Task SearchHistoricalLogs_ValidDateRange_ShouldReturnLogs()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var fromDate = today.AddDays(-7).ToString("yyyy-MM-dd");
        var toDate = today.ToString("yyyy-MM-dd");

        // Act
        var response = await adminClient.GetAsync(
            $"/api/admin/developer-logs/history/search?fromDate={fromDate}&toDate={toDate}&page=1&pageSize=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<LogEntriesPagedResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchHistoricalLogs_ExceedsMaxRange_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var fromDate = today.AddDays(-60).ToString("yyyy-MM-dd"); // More than 30 days
        var toDate = today.ToString("yyyy-MM-dd");

        // Act
        var response = await adminClient.GetAsync(
            $"/api/admin/developer-logs/history/search?fromDate={fromDate}&toDate={toDate}&page=1&pageSize=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchHistoricalLogs_InvalidDateFormat_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync(
            "/api/admin/developer-logs/history/search?fromDate=invalid&toDate=2024-01-01&page=1&pageSize=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchHistoricalLogs_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync(
            "/api/admin/developer-logs/history/search?fromDate=2024-01-01&toDate=2024-01-07&page=1&pageSize=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get Historical Log Size Tests

    [Fact]
    public async Task GetHistoricalLogSize_ValidDateRange_ShouldReturnSize()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var fromDate = today.AddDays(-7).ToString("yyyy-MM-dd");
        var toDate = today.ToString("yyyy-MM-dd");

        // Act
        var response = await adminClient.GetAsync(
            $"/api/admin/developer-logs/history/size?fromDate={fromDate}&toDate={toDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHistoricalLogSize_InvalidDateFormat_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync(
            "/api/admin/developer-logs/history/size?fromDate=invalid&toDate=2024-01-01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetHistoricalLogSize_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync(
            "/api/admin/developer-logs/history/size?fromDate=2024-01-01&toDate=2024-01-07");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task AllDeveloperLogEndpoints_ShouldRequireSystemAdminPermission()
    {
        // Arrange - Create a user without system admin permission
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act & Assert - All endpoints should return Forbidden
        var endpoints = new[]
        {
            ("/api/admin/developer-logs/level", "GET"),
            ("/api/admin/developer-logs/level/overrides", "GET"),
            ("/api/admin/developer-logs/buffer/stats", "GET"),
            ("/api/admin/developer-logs/buffer/entries?count=50", "GET"),
            ("/api/admin/developer-logs/buffer/errors?maxClusters=10", "GET"),
            ("/api/admin/developer-logs/history/dates", "GET"),
        };

        foreach (var (url, _) in endpoints)
        {
            var response = await userClient.GetAsync(url);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                $"Endpoint {url} should return Forbidden for non-admin user");
        }
    }

    #endregion
}
