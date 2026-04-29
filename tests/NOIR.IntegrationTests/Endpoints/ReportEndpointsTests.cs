using NOIR.Application.Features.Reports.DTOs;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for report endpoints.
/// Tests authentication, authorization, and basic endpoint functionality.
/// </summary>
[Collection("Integration")]
public class ReportEndpointsTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReportEndpointsTests(CustomWebApplicationFactory factory)
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

    private async Task<HttpClient> GetUnprivilegedUserClientAsync()
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

        var loginCommand = new LoginCommand(email, password);
        var loginResult = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginResponse = await loginResult.Content.ReadFromJsonAsync<LoginResponse>();
        return _factory.CreateAuthenticatedClient(loginResponse!.Auth!.AccessToken);
    }

    #region Authentication Tests

    [Fact]
    public async Task GetRevenueReport_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/reports/revenue");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBestSellersReport_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/reports/best-sellers");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetInventoryReport_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/reports/inventory");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCustomerReport_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/reports/customers");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task GetRevenueReport_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var userClient = await GetUnprivilegedUserClientAsync();

        // Act
        var response = await userClient.GetAsync("/api/reports/revenue");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetBestSellersReport_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var userClient = await GetUnprivilegedUserClientAsync();

        // Act
        var response = await userClient.GetAsync("/api/reports/best-sellers");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetInventoryReport_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var userClient = await GetUnprivilegedUserClientAsync();

        // Act
        var response = await userClient.GetAsync("/api/reports/inventory");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCustomerReport_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var userClient = await GetUnprivilegedUserClientAsync();

        // Act
        var response = await userClient.GetAsync("/api/reports/customers");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Revenue Report Tests

    [Fact]
    public async Task GetRevenueReport_AsAdmin_ShouldReturnOk()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/reports/revenue");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RevenueReportDto>();
        result.ShouldNotBeNull();
        result!.Period.ShouldNotBeNullOrEmpty();
        result.RevenueByDay.ShouldNotBeNull();
        result.RevenueByCategory.ShouldNotBeNull();
        result.RevenueByPaymentMethod.ShouldNotBeNull();
        result.ComparedToPreviousPeriod.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetRevenueReport_WithPeriodParameter_ShouldReturnOk()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/reports/revenue?period=weekly");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RevenueReportDto>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetRevenueReport_WithDateRange_ShouldReturnOk()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var startDate = DateTimeOffset.UtcNow.AddMonths(-1).ToString("o");
        var endDate = DateTimeOffset.UtcNow.ToString("o");

        // Act
        var response = await adminClient.GetAsync(
            $"/api/reports/revenue?startDate={Uri.EscapeDataString(startDate)}&endDate={Uri.EscapeDataString(endDate)}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RevenueReportDto>();
        result.ShouldNotBeNull();
    }

    #endregion

    #region Best Sellers Report Tests

    [Fact]
    public async Task GetBestSellersReport_AsAdmin_ShouldReturnOk()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/reports/best-sellers");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BestSellersReportDto>();
        result.ShouldNotBeNull();
        result!.Products.ShouldNotBeNull();
        result.Period.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetBestSellersReport_WithTopNParameter_ShouldReturnOk()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/reports/best-sellers?topN=5");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BestSellersReportDto>();
        result.ShouldNotBeNull();
        result!.Products.Count.ShouldBeLessThanOrEqualTo(5);
    }

    #endregion

    #region Inventory Report Tests

    [Fact]
    public async Task GetInventoryReport_AsAdmin_ShouldReturnOk()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/reports/inventory");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InventoryReportDto>();
        result.ShouldNotBeNull();
        result!.LowStockProducts.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetInventoryReport_WithThreshold_ShouldReturnOk()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/reports/inventory?lowStockThreshold=5");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InventoryReportDto>();
        result.ShouldNotBeNull();
    }

    #endregion

    #region Customer Report Tests

    [Fact]
    public async Task GetCustomerReport_AsAdmin_ShouldReturnOk()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/reports/customers");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CustomerReportDto>();
        result.ShouldNotBeNull();
        result!.AcquisitionByMonth.ShouldNotBeNull();
        result.TopCustomers.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetCustomerReport_WithDateRange_ShouldReturnOk()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var startDate = DateTimeOffset.UtcNow.AddMonths(-3).ToString("o");
        var endDate = DateTimeOffset.UtcNow.ToString("o");

        // Act
        var response = await adminClient.GetAsync(
            $"/api/reports/customers?startDate={Uri.EscapeDataString(startDate)}&endDate={Uri.EscapeDataString(endDate)}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CustomerReportDto>();
        result.ShouldNotBeNull();
    }

    #endregion

    #region Export Report Tests

    [Fact]
    public async Task ExportReport_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/reports/export?reportType=Revenue");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExportReport_AsAdmin_ShouldReturnFile()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/reports/export?reportType=Revenue&format=CSV");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull();
    }

    #endregion
}
