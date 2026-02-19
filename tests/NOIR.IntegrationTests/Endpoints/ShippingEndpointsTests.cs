using NOIR.Application.Features.Shipping.DTOs;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for shipping and shipping provider endpoints.
/// Tests authentication, authorization, and basic endpoint functionality.
/// </summary>
[Collection("Integration")]
public class ShippingEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ShippingEndpointsTests(CustomWebApplicationFactory factory)
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

    #region Shipping Provider - Authentication Tests

    [Fact]
    public async Task GetShippingProviders_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/shipping-providers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetActiveShippingProviders_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/shipping-providers/active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetShippingProviderById_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/shipping-providers/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Shipping Provider - Authorization Tests

    [Fact]
    public async Task GetShippingProviders_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var userClient = await GetUnprivilegedUserClientAsync();

        // Act
        var response = await userClient.GetAsync("/api/shipping-providers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetActiveShippingProviders_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var userClient = await GetUnprivilegedUserClientAsync();

        // Act
        var response = await userClient.GetAsync("/api/shipping-providers/active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetShippingProviderById_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var userClient = await GetUnprivilegedUserClientAsync();

        // Act
        var response = await userClient.GetAsync($"/api/shipping-providers/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Shipping Provider - Get All Tests

    [Fact]
    public async Task GetShippingProviders_AsAdmin_ShouldReturnOk()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/shipping-providers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<List<ShippingProviderDto>>();
        result.Should().NotBeNull();
    }

    #endregion

    #region Shipping Provider - Get Active Tests

    [Fact]
    public async Task GetActiveShippingProviders_AsAdmin_ShouldReturnOk()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/shipping-providers/active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<List<CheckoutShippingProviderDto>>();
        result.Should().NotBeNull();
    }

    #endregion

    #region Shipping Provider - Get By ID Tests

    [Fact]
    public async Task GetShippingProviderById_NonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/shipping-providers/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Shipping Operations - Authentication Tests

    [Fact]
    public async Task GetShippingOrder_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/shipping/orders/TRACK-12345");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetShippingOrderByOrderId_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/shipping/orders/by-order/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetShippingTracking_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/shipping/tracking/TRACK-12345");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Shipping Operations - Authorization Tests

    [Fact]
    public async Task GetShippingOrder_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var userClient = await GetUnprivilegedUserClientAsync();

        // Act
        var response = await userClient.GetAsync("/api/shipping/orders/TRACK-12345");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetShippingTracking_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var userClient = await GetUnprivilegedUserClientAsync();

        // Act
        var response = await userClient.GetAsync("/api/shipping/tracking/TRACK-12345");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Shipping Operations - Get Shipping Order Tests

    [Fact]
    public async Task GetShippingOrder_NonExistentTrackingNumber_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/shipping/orders/NON-EXISTENT-TRACKING");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetShippingOrderByOrderId_NonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/shipping/orders/by-order/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Shipping Operations - Tracking Tests

    [Fact]
    public async Task GetShippingTracking_NonExistentTrackingNumber_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/shipping/tracking/NON-EXISTENT-TRACKING");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Webhook Tests

    [Fact]
    public async Task ProcessWebhook_EmptyPayload_ShouldReturnOk()
    {
        // Arrange - Webhooks are public (AllowAnonymous)
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/shipping/webhooks/GHTK", content);

        // Assert
        // Webhooks always return 200 to prevent retries
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
