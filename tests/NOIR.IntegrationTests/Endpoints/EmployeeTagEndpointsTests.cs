using NOIR.Application.Features.Hr.DTOs;
using NOIR.Domain.Entities.Hr;
using NOIR.Domain.Enums;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for employee tag management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class EmployeeTagEndpointsTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EmployeeTagEndpointsTests(CustomWebApplicationFactory factory)
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

    #region GET /api/hr/tags

    [Fact]
    public async Task GetTags_AsAdmin_ShouldReturnList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/hr/tags");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<List<EmployeeTagDto>>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetTags_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/hr/tags");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTags_WithCategoryFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/hr/tags?category=Skill");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<List<EmployeeTagDto>>();
        result.ShouldNotBeNull();
    }

    #endregion

    #region GET /api/hr/tags/{id}

    [Fact]
    public async Task GetTagById_ValidId_ShouldReturnTag()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var tag = await CreateTestTagAsync(adminClient);

        // Act
        var response = await adminClient.GetAsync($"/api/hr/tags/{tag.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<EmployeeTagDto>();
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(tag.Id);
    }

    [Fact]
    public async Task GetTagById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/hr/tags/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/hr/tags

    [Fact]
    public async Task CreateTag_ValidRequest_ShouldReturnCreatedTag()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestTagRequest();

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/tags", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tag = await response.Content.ReadFromJsonWithEnumsAsync<EmployeeTagDto>();
        tag.ShouldNotBeNull();
        tag!.Name.ShouldBe(request.Name);
        tag.Category.ShouldBe(request.Category);
    }

    [Fact]
    public async Task CreateTag_DuplicateNameAndCategory_ShouldReturnConflict()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestTagRequest();

        // Create first tag
        var firstResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/tags", request);
        firstResponse.EnsureSuccessStatusCode();

        // Act - Try to create with same name and category
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/tags", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateTag_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = CreateTestTagRequest();

        // Act
        var response = await _client.PostAsJsonWithEnumsAsync("/api/hr/tags", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT /api/hr/tags/{id}

    [Fact]
    public async Task UpdateTag_ValidRequest_ShouldReturnUpdatedTag()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var tag = await CreateTestTagAsync(adminClient);

        var updateRequest = new UpdateEmployeeTagRequest(
            "Updated Tag Name",
            EmployeeTagCategory.Skill,
            Color: "#FF0000",
            Description: "Updated description");

        // Act
        var response = await adminClient.PutAsJsonWithEnumsAsync($"/api/hr/tags/{tag.Id}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonWithEnumsAsync<EmployeeTagDto>();
        updated.ShouldNotBeNull();
        updated!.Name.ShouldBe("Updated Tag Name");
        updated.Category.ShouldBe(EmployeeTagCategory.Skill);
    }

    [Fact]
    public async Task UpdateTag_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var updateRequest = new UpdateEmployeeTagRequest("Updated", EmployeeTagCategory.Team);

        // Act
        var response = await adminClient.PutAsJsonWithEnumsAsync($"/api/hr/tags/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/hr/tags/{id}

    [Fact]
    public async Task DeleteTag_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var tag = await CreateTestTagAsync(adminClient);

        // Act
        var response = await adminClient.DeleteAsync($"/api/hr/tags/{tag.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteTag_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/hr/tags/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/hr/tags/employees/{employeeId}/assign

    [Fact]
    public async Task AssignTagsToEmployee_ValidRequest_ShouldReturnTags()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var dept = await CreateTestDepartmentAsync(adminClient);
        var employee = await CreateTestEmployeeAsync(adminClient, dept.Id);
        var tag = await CreateTestTagAsync(adminClient);

        var request = new AssignTagsRequest(new List<Guid> { tag.Id });

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/hr/tags/employees/{employee.Id}/assign", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<List<TagBriefDto>>();
        result.ShouldNotBeNull();
        result!.ShouldContain(t => t.Id == tag.Id);
    }

    [Fact]
    public async Task AssignTagsToEmployee_InvalidEmployeeId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var tag = await CreateTestTagAsync(adminClient);
        var request = new AssignTagsRequest(new List<Guid> { tag.Id });

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/hr/tags/employees/{Guid.NewGuid()}/assign", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/hr/tags/employees/{employeeId}/remove

    [Fact]
    public async Task RemoveTagsFromEmployee_ValidRequest_ShouldReturnRemainingTags()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var dept = await CreateTestDepartmentAsync(adminClient);
        var employee = await CreateTestEmployeeAsync(adminClient, dept.Id);
        var tag = await CreateTestTagAsync(adminClient);

        // First assign
        var assignRequest = new AssignTagsRequest(new List<Guid> { tag.Id });
        await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/hr/tags/employees/{employee.Id}/assign", assignRequest);

        // Act - remove
        var removeRequest = new RemoveTagsRequest(new List<Guid> { tag.Id });
        var response = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/hr/tags/employees/{employee.Id}/remove", removeRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<List<TagBriefDto>>();
        result.ShouldNotBeNull();
        result!.ShouldNotContain(t => t.Id == tag.Id);
    }

    #endregion

    #region GET /api/hr/tags/{id}/employees

    [Fact]
    public async Task GetEmployeesByTag_ValidTagId_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var dept = await CreateTestDepartmentAsync(adminClient);
        var employee = await CreateTestEmployeeAsync(adminClient, dept.Id);
        var tag = await CreateTestTagAsync(adminClient);

        // Assign tag to employee
        var assignRequest = new AssignTagsRequest(new List<Guid> { tag.Id });
        await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/hr/tags/employees/{employee.Id}/assign", assignRequest);

        // Act
        var response = await adminClient.GetAsync($"/api/hr/tags/{tag.Id}/employees");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<EmployeeListDto>>();
        result.ShouldNotBeNull();
        result!.Items.ShouldContain(e => e.Id == employee.Id);
    }

    [Fact]
    public async Task GetEmployeesByTag_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/hr/tags/{Guid.NewGuid()}/employees");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region CRUD Lifecycle

    [Fact]
    public async Task Tag_FullCrudLifecycle_ShouldSucceed()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Create
        var createRequest = CreateTestTagRequest();
        var createResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/tags", createRequest);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonWithEnumsAsync<EmployeeTagDto>();
        created.ShouldNotBeNull();

        // Read
        var getResponse = await adminClient.GetAsync($"/api/hr/tags/{created!.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Update
        var updateRequest = new UpdateEmployeeTagRequest(
            "Lifecycle Updated Tag", EmployeeTagCategory.Custom,
            Color: "#00FF00");
        var updateResponse = await adminClient.PutAsJsonWithEnumsAsync($"/api/hr/tags/{created.Id}", updateRequest);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonWithEnumsAsync<EmployeeTagDto>();
        updated!.Name.ShouldBe("Lifecycle Updated Tag");

        // Delete
        var deleteResponse = await adminClient.DeleteAsync($"/api/hr/tags/{created.Id}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region DI Verification (Rule #21)

    [Fact]
    public async Task IRepository_EmployeeTag_ShouldResolveFromDI()
    {
        await _factory.ExecuteWithTenantAsync(sp =>
        {
            var repository = sp.GetRequiredService<IRepository<EmployeeTag, Guid>>();
            repository.ShouldNotBeNull();
            return Task.CompletedTask;
        });
    }

    #endregion

    #region Helper Methods

    private async Task<DepartmentDto> CreateTestDepartmentAsync(HttpClient adminClient)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var request = new CreateDepartmentRequest(
            $"Tag Test Dept {uniqueId}",
            $"TGD-{uniqueId}");
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/departments", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonWithEnumsAsync<DepartmentDto>())!;
    }

    private async Task<EmployeeDto> CreateTestEmployeeAsync(HttpClient adminClient, Guid departmentId)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var request = new CreateEmployeeRequest(
            $"Tag{uniqueId}",
            $"Test{uniqueId}",
            $"tag-emp-{uniqueId}@test.com",
            departmentId,
            DateTimeOffset.UtcNow,
            EmploymentType.FullTime);
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/employees", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonWithEnumsAsync<EmployeeDto>())!;
    }

    private async Task<EmployeeTagDto> CreateTestTagAsync(HttpClient adminClient)
    {
        var request = CreateTestTagRequest();
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/tags", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonWithEnumsAsync<EmployeeTagDto>())!;
    }

    private static CreateEmployeeTagRequest CreateTestTagRequest()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return new CreateEmployeeTagRequest(
            $"Test Tag {uniqueId}",
            EmployeeTagCategory.Skill,
            Color: "#3B82F6",
            Description: "Integration test tag");
    }

    #endregion
}
