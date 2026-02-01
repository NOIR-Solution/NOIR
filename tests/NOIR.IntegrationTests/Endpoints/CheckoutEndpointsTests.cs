namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for checkout endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class CheckoutEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CheckoutEndpointsTests(CustomWebApplicationFactory factory)
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

    #region InitiateCheckout Tests

    [Fact]
    public async Task InitiateCheckout_WithEmptyCart_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First clear the cart to ensure it's empty
        await adminClient.DeleteAsync("/api/cart");

        var command = new { CartId = Guid.NewGuid() };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/checkout/initiate", command);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InitiateCheckout_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new { CartId = Guid.NewGuid() };

        // Act
        var response = await _client.PostAsJsonAsync("/api/checkout/initiate", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetCheckoutSession Tests

    [Fact]
    public async Task GetCheckoutSession_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid();

        // Act
        var response = await adminClient.GetAsync($"/api/checkout/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region SetAddress Tests

    [Fact]
    public async Task SetAddress_WithInvalidSessionId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidSessionId = Guid.NewGuid();
        var command = new
        {
            AddressType = "Shipping",
            FullName = "Test User",
            Phone = "0123456789",
            AddressLine1 = "123 Test Street",
            City = "Test City",
            State = "Test State",
            PostalCode = "12345",
            Country = "VN"
        };

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/checkout/{invalidSessionId}/address", command);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion

    #region SelectShipping Tests

    [Fact]
    public async Task SelectShipping_WithInvalidSessionId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidSessionId = Guid.NewGuid();
        var command = new
        {
            ProviderId = Guid.NewGuid(),
            RateId = "standard"
        };

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/checkout/{invalidSessionId}/shipping", command);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion

    #region SelectPayment Tests

    [Fact]
    public async Task SelectPayment_WithInvalidSessionId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidSessionId = Guid.NewGuid();
        var command = new
        {
            GatewayId = Guid.NewGuid(),
            PaymentMethod = "CreditCard"
        };

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/checkout/{invalidSessionId}/payment", command);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion

    #region CompleteCheckout Tests

    [Fact]
    public async Task CompleteCheckout_WithInvalidSessionId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidSessionId = Guid.NewGuid();

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/checkout/{invalidSessionId}/complete", new { });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion
}
