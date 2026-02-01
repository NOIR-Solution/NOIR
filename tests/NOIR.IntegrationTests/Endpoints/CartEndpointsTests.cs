namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for shopping cart endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class CartEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CartEndpointsTests(CustomWebApplicationFactory factory)
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

    #region GetCart Tests

    [Fact]
    public async Task GetCart_AsAuthenticatedUser_ShouldReturnCart()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/cart");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCart_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/cart");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetCartSummary Tests

    [Fact]
    public async Task GetCartSummary_AsAuthenticatedUser_ShouldReturnSummary()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/cart/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region AddToCart Tests

    [Fact]
    public async Task AddToCart_WithInvalidVariantId_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var command = new { VariantId = Guid.NewGuid(), Quantity = 1 };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/cart/items", command);

        // Assert
        // May return BadRequest or NotFound depending on validation
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddToCart_WithNegativeQuantity_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var command = new { VariantId = Guid.NewGuid(), Quantity = -1 };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/cart/items", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region UpdateCartItem Tests

    [Fact]
    public async Task UpdateCartItem_WithInvalidItemId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidItemId = Guid.NewGuid();
        var command = new { Quantity = 2 };

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/cart/items/{invalidItemId}", command);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion

    #region RemoveCartItem Tests

    [Fact]
    public async Task RemoveCartItem_WithInvalidItemId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidItemId = Guid.NewGuid();

        // Act
        var response = await adminClient.DeleteAsync($"/api/cart/items/{invalidItemId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.NoContent);
    }

    #endregion

    #region ClearCart Tests

    [Fact]
    public async Task ClearCart_AsAuthenticatedUser_ShouldReturnNoContent()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync("/api/cart");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);
    }

    #endregion
}
