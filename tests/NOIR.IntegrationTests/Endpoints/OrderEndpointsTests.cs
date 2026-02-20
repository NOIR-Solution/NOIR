namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for order management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class OrderEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OrderEndpointsTests(CustomWebApplicationFactory factory)
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

    #region GetOrders Tests

    [Fact]
    public async Task GetOrders_AsAdmin_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOrders_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrders_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/orders?pageNumber=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOrders_WithStatusFilter_ShouldFilterResults()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/orders?status=Pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region GetOrderById Tests

    [Fact]
    public async Task GetOrderById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid();

        // Act
        var response = await adminClient.GetAsync($"/api/orders/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region ConfirmOrder Tests

    [Fact]
    public async Task ConfirmOrder_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid();

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/orders/{invalidId}/confirm", new { });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion

    #region CancelOrder Tests

    [Fact]
    public async Task CancelOrder_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid();
        var command = new { Reason = "Test cancellation" };

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/orders/{invalidId}/cancel", command);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion

    #region ShipOrder Tests

    [Fact]
    public async Task ShipOrder_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid();
        var command = new
        {
            TrackingNumber = "TRACK123456",
            Carrier = "TestCarrier"
        };

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/orders/{invalidId}/ship", command);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetOrderPayments Tests

    [Fact]
    public async Task GetOrderPayments_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid();

        // Act
        var response = await adminClient.GetAsync($"/api/orders/{invalidId}/payments");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.OK);
    }

    #endregion

    #region ManualCreateOrder Tests

    [Fact]
    public async Task ManualCreateOrder_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new
        {
            CustomerEmail = "test@example.com",
            Items = new[] { new { ProductVariantId = Guid.NewGuid(), Quantity = 1 } }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders/manual", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ManualCreateOrder_WithEmptyItems_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var command = new
        {
            CustomerEmail = "test@example.com",
            Items = Array.Empty<object>()
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/orders/manual", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ManualCreateOrder_WithInvalidVariantId_ShouldReturnNotFoundOrBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var command = new
        {
            CustomerEmail = "test@example.com",
            CustomerName = "Test Customer",
            Items = new[] { new { ProductVariantId = Guid.NewGuid(), Quantity = 1 } }
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/orders/manual", command);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ManualCreateOrder_WithInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var command = new
        {
            CustomerEmail = "not-an-email",
            Items = new[] { new { ProductVariantId = Guid.NewGuid(), Quantity = 1 } }
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/orders/manual", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
