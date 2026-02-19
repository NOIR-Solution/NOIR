using NOIR.Application.Features.Customers.Commands.CreateCustomer;
using NOIR.Application.Features.Customers.Commands.UpdateCustomer;
using NOIR.Application.Features.Customers.DTOs;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for customer management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class CustomerEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CustomerEndpointsTests(CustomWebApplicationFactory factory)
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

    #region GET /api/customers

    [Fact]
    public async Task GetCustomers_AsAdmin_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/customers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<CustomerSummaryDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCustomers_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/customers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCustomers_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/customers?page=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<CustomerSummaryDto>>();
        result.Should().NotBeNull();
        result!.Items.Count.Should().BeLessThanOrEqualTo(5);
    }

    #endregion

    #region GET /api/customers/{id}

    [Fact]
    public async Task GetCustomerById_ValidId_ShouldReturnCustomer()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a customer
        var createRequest = CreateTestCustomerCommand();
        var createResponse = await adminClient.PostAsJsonAsync("/api/customers", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdCustomer = await createResponse.Content.ReadFromJsonWithEnumsAsync<CustomerDto>();

        // Act
        var response = await adminClient.GetAsync($"/api/customers/{createdCustomer!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var customer = await response.Content.ReadFromJsonWithEnumsAsync<CustomerDto>();
        customer.Should().NotBeNull();
        customer!.Id.Should().Be(createdCustomer.Id);
        customer.Email.Should().Be(createRequest.Email);
        customer.FirstName.Should().Be(createRequest.FirstName);
        customer.LastName.Should().Be(createRequest.LastName);
    }

    [Fact]
    public async Task GetCustomerById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/customers/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /api/customers/stats

    [Fact]
    public async Task GetCustomerStats_AsAdmin_ShouldReturnStats()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/customers/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<CustomerStatsDto>();
        result.Should().NotBeNull();
        result!.SegmentDistribution.Should().NotBeNull();
        result.TierDistribution.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCustomerStats_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/customers/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/customers

    [Fact]
    public async Task CreateCustomer_ValidRequest_ShouldReturnCreatedCustomer()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestCustomerCommand();

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/customers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var customer = await response.Content.ReadFromJsonWithEnumsAsync<CustomerDto>();
        customer.Should().NotBeNull();
        customer!.Email.Should().Be(request.Email);
        customer.FirstName.Should().Be(request.FirstName);
        customer.LastName.Should().Be(request.LastName);
    }

    [Fact]
    public async Task CreateCustomer_DuplicateEmail_ShouldReturnConflict()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestCustomerCommand();

        // Create the first customer
        var firstResponse = await adminClient.PostAsJsonAsync("/api/customers", request);
        firstResponse.EnsureSuccessStatusCode();

        // Act - Try to create with same email
        var response = await adminClient.PostAsJsonAsync("/api/customers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateCustomer_EmptyEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new CreateCustomerCommand(
            Email: "",
            FirstName: "Test",
            LastName: "Customer");

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/customers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCustomer_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = CreateTestCustomerCommand();

        // Act
        var response = await _client.PostAsJsonAsync("/api/customers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT /api/customers/{id}

    [Fact]
    public async Task UpdateCustomer_ValidRequest_ShouldReturnUpdatedCustomer()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a customer
        var createRequest = CreateTestCustomerCommand();
        var createResponse = await adminClient.PostAsJsonAsync("/api/customers", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdCustomer = await createResponse.Content.ReadFromJsonWithEnumsAsync<CustomerDto>();

        // Update the customer
        var updateRequest = new UpdateCustomerCommand(
            Id: createdCustomer!.Id,
            Email: createdCustomer.Email,
            FirstName: "Updated",
            LastName: "Name",
            Phone: "+84912345678",
            Tags: "vip",
            Notes: "Updated notes");

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/customers/{createdCustomer.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedCustomer = await response.Content.ReadFromJsonWithEnumsAsync<CustomerDto>();
        updatedCustomer.Should().NotBeNull();
        updatedCustomer!.FirstName.Should().Be("Updated");
        updatedCustomer.LastName.Should().Be("Name");
    }

    [Fact]
    public async Task UpdateCustomer_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var updateRequest = new UpdateCustomerCommand(
            Id: Guid.NewGuid(),
            Email: "nonexistent@example.com",
            FirstName: "Updated",
            LastName: "Name");

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/customers/{updateRequest.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/customers/{id}

    [Fact]
    public async Task DeleteCustomer_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a customer
        var request = CreateTestCustomerCommand();
        var createResponse = await adminClient.PostAsJsonAsync("/api/customers", request);
        createResponse.EnsureSuccessStatusCode();
        var createdCustomer = await createResponse.Content.ReadFromJsonWithEnumsAsync<CustomerDto>();

        // Act
        var response = await adminClient.DeleteAsync($"/api/customers/{createdCustomer!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify it's deleted (soft delete - should return not found)
        var getResponse = await adminClient.GetAsync($"/api/customers/{createdCustomer.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCustomer_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/customers/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Helper Methods

    private static CreateCustomerCommand CreateTestCustomerCommand()
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        return new CreateCustomerCommand(
            Email: $"customer_{uniqueId}@example.com",
            FirstName: "Test",
            LastName: $"Customer_{uniqueId}",
            Phone: "+84900000000",
            Tags: "test",
            Notes: "Integration test customer");
    }

    #endregion
}
