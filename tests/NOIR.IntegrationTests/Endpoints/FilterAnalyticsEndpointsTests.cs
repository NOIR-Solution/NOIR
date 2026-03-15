using NOIR.Application.Features.FilterAnalytics.DTOs;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for filter analytics endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class FilterAnalyticsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public FilterAnalyticsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    private async Task<HttpClient> GetAdminClientAsync()
    {
        var loginCommand = new LoginCommand("admin@noir.local", "123qwe");
        var response = await _client.PostAsJsonWithEnumsAsync("/api/auth/login", loginCommand);
        var loginResponse = await response.Content.ReadFromJsonWithEnumsAsync<LoginResponse>();
        return _factory.CreateAuthenticatedClient(loginResponse!.Auth!.AccessToken);
    }

    #region POST /api/analytics/filter-events

    [Fact]
    public async Task TrackFilterEvent_ValidRequest_ShouldReturn200()
    {
        // Arrange - This endpoint allows anonymous access
        var command = new
        {
            SessionId = Guid.NewGuid().ToString(),
            EventType = "FilterApplied",
            ProductCount = 10,
            CategorySlug = "electronics",
            FilterCode = "color",
            FilterValue = "red"
        };

        // Act
        var response = await _client.PostAsJsonWithEnumsAsync("/api/analytics/filter-events", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TrackFilterEvent_AsAdmin_ShouldReturn200()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var command = new
        {
            SessionId = Guid.NewGuid().ToString(),
            EventType = "FilterApplied",
            ProductCount = 5,
            FilterCode = "size",
            FilterValue = "M"
        };

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/analytics/filter-events", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region GET /api/analytics/filter-events/popular

    [Fact]
    public async Task GetPopularFilters_AsAdmin_ShouldReturn200()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/analytics/filter-events/popular");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PopularFiltersResult>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetPopularFilters_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/analytics/filter-events/popular");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPopularFilters_WithDateRange_ShouldReturn200()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var from = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-30).ToString("o"));
        var to = Uri.EscapeDataString(DateTimeOffset.UtcNow.ToString("o"));

        // Act
        var response = await adminClient.GetAsync($"/api/analytics/filter-events/popular?fromDate={from}&toDate={to}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPopularFilters_WithTopLimit_ShouldReturn200()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/analytics/filter-events/popular?top=5");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion
}
