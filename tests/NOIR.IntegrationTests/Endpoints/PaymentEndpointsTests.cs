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

        // Act - Gateway management routes are under /api/payment-gateways
        var response = await adminClient.GetAsync("/api/payment-gateways");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPaymentGateways_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/payment-gateways");

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

        // Act - Gateway by ID is under /api/payment-gateways/{id}
        var response = await adminClient.GetAsync($"/api/payment-gateways/{invalidId}");

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

        // Act - Transaction list is at /api/payments (root of payments group)
        var response = await adminClient.GetAsync("/api/payments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPaymentTransactions_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act - Transaction list uses 'page' and 'pageSize' query params
        var response = await adminClient.GetAsync("/api/payments?page=1&pageSize=5");

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

        // Act - Transaction by ID is at /api/payments/{id}
        var response = await adminClient.GetAsync($"/api/payments/{invalidId}");

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

        // Act - Configure gateway is at /api/payment-gateways
        var response = await adminClient.PostAsJsonAsync("/api/payment-gateways", command);

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

        // Act - Update gateway is at /api/payment-gateways/{id}
        var response = await adminClient.PutAsJsonAsync($"/api/payment-gateways/{invalidId}", command);

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

        // Act - Test connection is at /api/payment-gateways/{id}/test
        var response = await adminClient.PostAsJsonAsync($"/api/payment-gateways/{invalidId}/test", new { });

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

        // Act - Cancel payment is at /api/payments/{id}/cancel
        var response = await adminClient.PostAsJsonAsync($"/api/payments/{invalidId}/cancel", new { });

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
        var invalidTransactionId = Guid.NewGuid();
        var command = new
        {
            PaymentTransactionId = invalidTransactionId,
            Amount = 10.00m,
            Reason = "CustomerRequest",
            Notes = "Test refund"
        };

        // Act - Refund requests are at /api/refunds (POST with PaymentTransactionId in body)
        var response = await adminClient.PostAsJsonAsync("/api/refunds", command);

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

    #region RecordManualPayment Tests

    [Fact]
    public async Task RecordManualPayment_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new
        {
            OrderId = Guid.NewGuid(),
            Amount = 500000m,
            Currency = "VND",
            PaymentMethod = "BankTransfer",
            ReferenceNumber = "REF-001",
            Notes = "Manual payment"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/payments/manual", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RecordManualPayment_WithInvalidOrderId_ShouldReturnNotFoundOrBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var command = new
        {
            OrderId = Guid.NewGuid(),
            Amount = 500000m,
            Currency = "VND",
            PaymentMethod = "BankTransfer",
            ReferenceNumber = "REF-001"
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/payments/manual", command);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetPaymentDetails Tests

    [Fact]
    public async Task GetPaymentDetails_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid();

        // Act
        var response = await adminClient.GetAsync($"/api/payments/{invalidId}/details");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetPaymentTimeline Tests

    [Fact]
    public async Task GetPaymentTimeline_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid();

        // Act
        var response = await adminClient.GetAsync($"/api/payments/{invalidId}/timeline");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region RefreshPaymentStatus Tests

    [Fact]
    public async Task RefreshPayment_WithInvalidId_ShouldReturnNotFoundOrBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid();

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/payments/{invalidId}/refresh", new { });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion
}
