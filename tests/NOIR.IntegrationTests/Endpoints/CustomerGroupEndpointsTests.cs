using NOIR.Application.Features.CustomerGroups.DTOs;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for customer group management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class CustomerGroupEndpointsTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CustomerGroupEndpointsTests(CustomWebApplicationFactory factory)
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

    #region GET /api/customer-groups

    [Fact]
    public async Task GetCustomerGroups_AsAdmin_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/customer-groups");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CustomerGroupListDto>>();
        result.ShouldNotBeNull();
        result!.Items.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetCustomerGroups_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/customer-groups");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCustomerGroups_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/customer-groups?pageNumber=1&pageSize=5");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CustomerGroupListDto>>();
        result.ShouldNotBeNull();
        result!.Items.Count.ShouldBeLessThanOrEqualTo(5);
    }

    #endregion

    #region GET /api/customer-groups/{id}

    [Fact]
    public async Task GetCustomerGroupById_ValidId_ShouldReturnGroup()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a customer group
        var createRequest = CreateTestGroupRequest();
        var createResponse = await adminClient.PostAsJsonAsync("/api/customer-groups", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdGroup = await createResponse.Content.ReadFromJsonAsync<CustomerGroupDto>();

        // Act
        var response = await adminClient.GetAsync($"/api/customer-groups/{createdGroup!.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var group = await response.Content.ReadFromJsonAsync<CustomerGroupDto>();
        group.ShouldNotBeNull();
        group!.Id.ShouldBe(createdGroup.Id);
        group.Name.ShouldBe(createRequest.Name);
    }

    [Fact]
    public async Task GetCustomerGroupById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/customer-groups/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/customer-groups

    [Fact]
    public async Task CreateCustomerGroup_ValidRequest_ShouldReturnCreatedGroup()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestGroupRequest();

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/customer-groups", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var group = await response.Content.ReadFromJsonAsync<CustomerGroupDto>();
        group.ShouldNotBeNull();
        group!.Name.ShouldBe(request.Name);
        group.Description.ShouldBe(request.Description);
        group.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateCustomerGroup_DuplicateName_ShouldReturnConflict()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestGroupRequest();

        // Create the first group
        var firstResponse = await adminClient.PostAsJsonAsync("/api/customer-groups", request);
        firstResponse.EnsureSuccessStatusCode();

        // Act - Try to create with same name
        var response = await adminClient.PostAsJsonAsync("/api/customer-groups", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateCustomerGroup_EmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new CreateCustomerGroupRequest(
            Name: "",
            Description: "Test description");

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/customer-groups", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCustomerGroup_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = CreateTestGroupRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/customer-groups", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT /api/customer-groups/{id}

    [Fact]
    public async Task UpdateCustomerGroup_ValidRequest_ShouldReturnUpdatedGroup()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a customer group
        var createRequest = CreateTestGroupRequest();
        var createResponse = await adminClient.PostAsJsonAsync("/api/customer-groups", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdGroup = await createResponse.Content.ReadFromJsonAsync<CustomerGroupDto>();

        // Update the group
        var updateRequest = new UpdateCustomerGroupRequest(
            Name: "Updated Group Name",
            Description: "Updated description",
            IsActive: true);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/customer-groups/{createdGroup!.Id}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updatedGroup = await response.Content.ReadFromJsonAsync<CustomerGroupDto>();
        updatedGroup.ShouldNotBeNull();
        updatedGroup!.Name.ShouldBe("Updated Group Name");
        updatedGroup.Description.ShouldBe("Updated description");
    }

    [Fact]
    public async Task UpdateCustomerGroup_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var updateRequest = new UpdateCustomerGroupRequest(
            Name: "Updated Group",
            Description: "Updated description",
            IsActive: true);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/customer-groups/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/customer-groups/{id}

    [Fact]
    public async Task DeleteCustomerGroup_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a customer group
        var request = CreateTestGroupRequest();
        var createResponse = await adminClient.PostAsJsonAsync("/api/customer-groups", request);
        createResponse.EnsureSuccessStatusCode();
        var createdGroup = await createResponse.Content.ReadFromJsonAsync<CustomerGroupDto>();

        // Act
        var response = await adminClient.DeleteAsync($"/api/customer-groups/{createdGroup!.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify it's deleted (soft delete - should return not found)
        var getResponse = await adminClient.GetAsync($"/api/customer-groups/{createdGroup.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCustomerGroup_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/customer-groups/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region DI Verification (Rule #21)

    [Fact]
    public async Task IRepository_CustomerGroup_ShouldResolveFromDI()
    {
        // Rule #21: Repository implementations need DI verification
        await _factory.ExecuteWithTenantAsync(sp =>
        {
            var repository = sp.GetRequiredService<IRepository<CustomerGroup, Guid>>();
            repository.ShouldNotBeNull();
            return Task.CompletedTask;
        });
    }

    #endregion

    #region Helper Methods

    private static CreateCustomerGroupRequest CreateTestGroupRequest()
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        return new CreateCustomerGroupRequest(
            Name: $"Test Group {uniqueId}",
            Description: "Integration test customer group");
    }

    #endregion
}
