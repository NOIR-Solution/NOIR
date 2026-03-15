using NOIR.Application.Features.Hr.DTOs;
using NOIR.Domain.Entities.Hr;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for department management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class DepartmentEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DepartmentEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    private async Task<HttpClient> GetAdminClientAsync()
    {
        var loginCommand = new LoginCommand("admin@noir.local", "123qwe");
        var response = await _client.PostAsJsonWithEnumsAsync("/api/auth/login", loginCommand);
        var loginResponse = await response.Content.ReadFromJsonWithEnumsAsync<LoginResponse>();
        return _factory.CreateAuthenticatedClient(loginResponse!.Auth!.AccessToken);
    }

    #region GET /api/hr/departments

    [Fact]
    public async Task GetDepartments_AsAdmin_ShouldReturnTree()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/hr/departments");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<List<DepartmentTreeNodeDto>>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetDepartments_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/hr/departments");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDepartments_WithIncludeInactive_ShouldReturnResults()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/hr/departments?includeInactive=true");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<List<DepartmentTreeNodeDto>>();
        result.ShouldNotBeNull();
    }

    #endregion

    #region GET /api/hr/departments/{id}

    [Fact]
    public async Task GetDepartmentById_ValidId_ShouldReturnDepartment()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var dept = await CreateTestDepartmentAsync(adminClient);

        // Act
        var response = await adminClient.GetAsync($"/api/hr/departments/{dept.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<DepartmentDto>();
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(dept.Id);
        result.Name.ShouldBe(dept.Name);
    }

    [Fact]
    public async Task GetDepartmentById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/hr/departments/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDepartmentById_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/hr/departments/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/hr/departments

    [Fact]
    public async Task CreateDepartment_ValidRequest_ShouldReturnCreatedDepartment()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestDepartmentRequest();

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/departments", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dept = await response.Content.ReadFromJsonWithEnumsAsync<DepartmentDto>();
        dept.ShouldNotBeNull();
        dept!.Name.ShouldBe(request.Name);
        dept.Code.ShouldBe(request.Code);
    }

    [Fact]
    public async Task CreateDepartment_DuplicateCode_ShouldReturnConflict()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestDepartmentRequest();

        // Create first department
        var firstResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/departments", request);
        firstResponse.EnsureSuccessStatusCode();

        // Act - Try to create with same code
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/departments", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateDepartment_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = CreateTestDepartmentRequest();

        // Act
        var response = await _client.PostAsJsonWithEnumsAsync("/api/hr/departments", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateDepartment_WithParent_ShouldReturnDepartmentWithParent()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var parent = await CreateTestDepartmentAsync(adminClient);

        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var request = new CreateDepartmentRequest(
            $"Sub Dept {uniqueId}",
            $"SUB-{uniqueId}",
            Description: "A sub-department",
            ParentDepartmentId: parent.Id);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/departments", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dept = await response.Content.ReadFromJsonWithEnumsAsync<DepartmentDto>();
        dept.ShouldNotBeNull();
        dept!.ParentDepartmentId.ShouldBe(parent.Id);
    }

    #endregion

    #region PUT /api/hr/departments/{id}

    [Fact]
    public async Task UpdateDepartment_ValidRequest_ShouldReturnUpdatedDepartment()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var dept = await CreateTestDepartmentAsync(adminClient);

        var updateRequest = new UpdateDepartmentRequest(
            "Updated Department",
            dept.Code,
            Description: "Updated description");

        // Act
        var response = await adminClient.PutAsJsonWithEnumsAsync($"/api/hr/departments/{dept.Id}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonWithEnumsAsync<DepartmentDto>();
        updated.ShouldNotBeNull();
        updated!.Name.ShouldBe("Updated Department");
        updated.Description.ShouldBe("Updated description");
    }

    [Fact]
    public async Task UpdateDepartment_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var updateRequest = new UpdateDepartmentRequest("Updated", "UPD-001");

        // Act
        var response = await adminClient.PutAsJsonWithEnumsAsync($"/api/hr/departments/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/hr/departments/{id}

    [Fact]
    public async Task DeleteDepartment_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var dept = await CreateTestDepartmentAsync(adminClient);

        // Act
        var response = await adminClient.DeleteAsync($"/api/hr/departments/{dept.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify it's deleted (soft delete - should return not found)
        var getResponse = await adminClient.GetAsync($"/api/hr/departments/{dept.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteDepartment_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/hr/departments/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region CRUD Lifecycle

    [Fact]
    public async Task Department_FullCrudLifecycle_ShouldSucceed()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Create
        var createRequest = CreateTestDepartmentRequest();
        var createResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/departments", createRequest);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonWithEnumsAsync<DepartmentDto>();
        created.ShouldNotBeNull();

        // Read
        var getResponse = await adminClient.GetAsync($"/api/hr/departments/{created!.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Update
        var updateRequest = new UpdateDepartmentRequest(
            "Lifecycle Updated", created.Code,
            Description: "Updated via lifecycle test");
        var updateResponse = await adminClient.PutAsJsonWithEnumsAsync($"/api/hr/departments/{created.Id}", updateRequest);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonWithEnumsAsync<DepartmentDto>();
        updated!.Name.ShouldBe("Lifecycle Updated");

        // Delete
        var deleteResponse = await adminClient.DeleteAsync($"/api/hr/departments/{created.Id}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region DI Verification (Rule #21)

    [Fact]
    public async Task IRepository_Department_ShouldResolveFromDI()
    {
        await _factory.ExecuteWithTenantAsync(sp =>
        {
            var repository = sp.GetRequiredService<IRepository<Department, Guid>>();
            repository.ShouldNotBeNull();
            return Task.CompletedTask;
        });
    }

    #endregion

    #region Helper Methods

    private async Task<DepartmentDto> CreateTestDepartmentAsync(HttpClient adminClient)
    {
        var request = CreateTestDepartmentRequest();
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/departments", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonWithEnumsAsync<DepartmentDto>())!;
    }

    private static CreateDepartmentRequest CreateTestDepartmentRequest()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return new CreateDepartmentRequest(
            $"Test Dept {uniqueId}",
            $"DEPT-{uniqueId}",
            Description: "Integration test department");
    }

    #endregion
}
