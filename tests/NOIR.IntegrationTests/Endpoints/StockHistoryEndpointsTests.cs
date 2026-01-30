using NOIR.Application.Features.Inventory.DTOs;
using StockHistoryPagedResult = NOIR.Application.Features.Products.Queries.GetProducts.PagedResult<NOIR.Application.Features.Inventory.DTOs.InventoryMovementDto>;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for stock history endpoints.
/// Tests authentication, authorization, and basic endpoint functionality.
/// Handler logic is thoroughly tested in GetStockHistoryQueryHandlerTests.
/// </summary>
[Collection("Integration")]
public class StockHistoryEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public StockHistoryEndpointsTests(CustomWebApplicationFactory factory)
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

    #region Authentication and Authorization Tests

    [Fact]
    public async Task GetStockHistory_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync(
            $"/api/products/{Guid.NewGuid()}/variants/{Guid.NewGuid()}/stock-history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStockHistory_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without admin permissions
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
        var loginResult = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginResponse = await loginResult.Content.ReadFromJsonAsync<LoginResponse>();
        var userClient = _factory.CreateAuthenticatedClient(loginResponse!.Auth!.AccessToken);

        // Act
        var response = await userClient.GetAsync(
            $"/api/products/{Guid.NewGuid()}/variants/{Guid.NewGuid()}/stock-history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Endpoint Availability Tests

    [Fact]
    public async Task GetStockHistory_WithValidAuth_ShouldReturnOkOrEmpty()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act - Use random IDs since we're just testing endpoint availability
        // The endpoint should return 200 with empty results, or possibly 404 for non-existent product
        var response = await adminClient.GetAsync(
            $"/api/products/{Guid.NewGuid()}/variants/{Guid.NewGuid()}/stock-history");

        // Assert - endpoint should return 200 OK with empty results
        // (The specification filters by productId + variantId, so non-existent IDs just return empty)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonWithEnumsAsync<StockHistoryPagedResult>();
            result.Should().NotBeNull();
            result!.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task GetStockHistory_WithPaginationParams_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync(
            $"/api/products/{Guid.NewGuid()}/variants/{Guid.NewGuid()}/stock-history?page=2&pageSize=10");

        // Assert - endpoint should return 200 OK with pagination info
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonWithEnumsAsync<StockHistoryPagedResult>();
            result.Should().NotBeNull();
            result!.Page.Should().Be(2);
            result.PageSize.Should().Be(10);
        }
    }

    #endregion
}
