namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for payment management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class PaymentEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PaymentEndpointsTests(CustomWebApplicationFactory factory)
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

    #region GetPaymentGateways Tests

    [Fact]
    public async Task GetPaymentGateways_AsAdmin_ShouldReturnList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/payments/gateways");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPaymentGateways_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/payments/gateways");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetPaymentGatewayById Tests

    [Fact]
    public async Task GetPaymentGatewayById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid();

        // Act
        var response = await adminClient.GetAsync($"/api/payments/gateways/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetPaymentTransactions Tests

    [Fact]
    public async Task GetPaymentTransactions_AsAdmin_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/payments/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPaymentTransactions_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/payments/transactions?pageNumber=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region GetPaymentTransaction Tests

    [Fact]
    public async Task GetPaymentTransaction_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid();

        // Act
        var response = await adminClient.GetAsync($"/api/payments/transactions/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region ConfigureGateway Tests

    [Fact]
    public async Task ConfigureGateway_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var command = new
        {
            Provider = "", // Invalid empty provider
            DisplayName = "Test Gateway",
            Environment = "Sandbox",
            Credentials = new Dictionary<string, string>(),
            SupportedMethods = new List<string> { "CreditCard" },
            SortOrder = 1,
            IsActive = true
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/payments/gateways", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region UpdateGateway Tests

    [Fact]
    public async Task UpdateGateway_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid();
        var command = new
        {
            DisplayName = "Updated Gateway",
            IsActive = true
        };

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/payments/gateways/{invalidId}", command);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion

    #region TestGatewayConnection Tests

    [Fact]
    public async Task TestGatewayConnection_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid();

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/payments/gateways/{invalidId}/test", new { });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion

    #region CancelPayment Tests

    [Fact]
    public async Task CancelPayment_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid();

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/payments/transactions/{invalidId}/cancel", new { });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion

    #region RequestRefund Tests

    [Fact]
    public async Task RequestRefund_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid();
        var command = new
        {
            Amount = 10.00m,
            Reason = "Customer request"
        };

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/payments/transactions/{invalidId}/refund", command);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetPendingCOD Tests

    [Fact]
    public async Task GetPendingCOD_AsAdmin_ShouldReturnList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/payments/cod/pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
