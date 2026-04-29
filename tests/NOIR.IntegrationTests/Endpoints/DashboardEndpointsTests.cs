using NOIR.Application.Features.Dashboard.DTOs;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for dashboard endpoints.
/// Tests the full HTTP request/response cycle for dashboard metrics retrieval.
/// </summary>
[Collection("Integration")]
public class DashboardEndpointsTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DashboardEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    private async Task<HttpClient> GetAdminClientAsync()
    {
        var loginCommand = new LoginCommand("admin@noir.local", "123qwe");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return _factory.CreateAuthenticatedClient(loginResponse!.Auth!.AccessToken);
    }

    [Fact]
    public async Task GetDashboardMetrics_AsAdmin_ShouldReturnMetrics()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/dashboard/metrics");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DashboardMetricsDto>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetDashboardMetrics_Unauthenticated_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/dashboard/metrics");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboardMetrics_WithCustomParameters_ShouldReturnMetrics()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/dashboard/metrics?topProducts=3&salesDays=7");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DashboardMetricsDto>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetDashboardMetrics_ShouldReturnAllSections()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/dashboard/metrics");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DashboardMetricsDto>();
        result.ShouldNotBeNull();

        // Verify all sections are present
        result!.Revenue.ShouldNotBeNull("Revenue section should be present");
        result.OrderCounts.ShouldNotBeNull("OrderCounts section should be present");
        result.TopSellingProducts.ShouldNotBeNull("TopSellingProducts section should be present");
        result.LowStockProducts.ShouldNotBeNull("LowStockProducts section should be present");
        result.RecentOrders.ShouldNotBeNull("RecentOrders section should be present");
        result.SalesOverTime.ShouldNotBeNull("SalesOverTime section should be present");
        result.ProductDistribution.ShouldNotBeNull("ProductDistribution section should be present");
    }
}
