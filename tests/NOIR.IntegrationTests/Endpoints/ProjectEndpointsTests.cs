using NOIR.Application.Features.Hr.DTOs;
using NOIR.Application.Features.Pm.DTOs;
using NOIR.Domain.Entities.Pm;
using NOIR.Domain.Enums;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for project management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class ProjectEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProjectEndpointsTests(CustomWebApplicationFactory factory)
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

    #region GET /api/pm/projects

    [Fact]
    public async Task GetProjects_AsAdmin_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/pm/projects");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<ProjectListDto>>();
        result.ShouldNotBeNull();
        result!.Items.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetProjects_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/pm/projects");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProjects_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/pm/projects?page=1&pageSize=5");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<ProjectListDto>>();
        result.ShouldNotBeNull();
        result!.Items.Count.ShouldBeLessThanOrEqualTo(5);
    }

    #endregion

    #region GET /api/pm/projects/search

    [Fact]
    public async Task SearchProjects_AsAdmin_ShouldReturnResults()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/pm/projects/search?q=test");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<List<ProjectSearchDto>>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task SearchProjects_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/pm/projects/search?q=test");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/pm/projects/{id}

    [Fact]
    public async Task GetProjectById_ValidId_ShouldReturnProject()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);

        // Act
        var response = await adminClient.GetAsync($"/api/pm/projects/{project.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<ProjectDto>();
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(project.Id);
    }

    [Fact]
    public async Task GetProjectById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/pm/projects/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProjectById_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/pm/projects/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/pm/projects

    [Fact]
    public async Task CreateProject_ValidRequest_ShouldReturnCreatedProject()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestProjectRequest();

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/pm/projects", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var project = await response.Content.ReadFromJsonWithEnumsAsync<ProjectDto>();
        project.ShouldNotBeNull();
        project!.Name.ShouldBe(request.Name);
        project.ProjectCode.ShouldStartWith("PRJ-");
    }

    [Fact]
    public async Task CreateProject_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = CreateTestProjectRequest();

        // Act
        var response = await _client.PostAsJsonWithEnumsAsync("/api/pm/projects", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT /api/pm/projects/{id}

    [Fact]
    public async Task UpdateProject_ValidRequest_ShouldReturnUpdatedProject()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);

        var updateRequest = new UpdateProjectRequest(
            "Updated Project Name",
            Description: "Updated description",
            Budget: 50000m,
            Currency: "USD");

        // Act
        var response = await adminClient.PutAsJsonWithEnumsAsync($"/api/pm/projects/{project.Id}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonWithEnumsAsync<ProjectDto>();
        updated.ShouldNotBeNull();
        updated!.Name.ShouldBe("Updated Project Name");
        updated.Description.ShouldBe("Updated description");
    }

    [Fact]
    public async Task UpdateProject_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var updateRequest = new UpdateProjectRequest("Updated Project");

        // Act
        var response = await adminClient.PutAsJsonWithEnumsAsync($"/api/pm/projects/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/pm/projects/{id}/archive

    [Fact]
    public async Task ArchiveProject_ValidId_ShouldReturnArchivedProject()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync($"/api/pm/projects/{project.Id}/archive", new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<ProjectDto>();
        result.ShouldNotBeNull();
        result!.Status.ShouldBe(ProjectStatus.Archived);
    }

    [Fact]
    public async Task ArchiveProject_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync($"/api/pm/projects/{Guid.NewGuid()}/archive", new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/pm/projects/{id}/status

    [Fact]
    public async Task ChangeProjectStatus_ValidRequest_ShouldReturnUpdatedProject()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);

        var statusRequest = new ChangeProjectStatusRequest(ProjectStatus.OnHold);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync($"/api/pm/projects/{project.Id}/status", statusRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<ProjectDto>();
        result.ShouldNotBeNull();
        result!.Status.ShouldBe(ProjectStatus.OnHold);
    }

    [Fact]
    public async Task ChangeProjectStatus_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var statusRequest = new ChangeProjectStatusRequest(ProjectStatus.Completed);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync($"/api/pm/projects/{Guid.NewGuid()}/status", statusRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/pm/projects/{id}

    [Fact]
    public async Task DeleteProject_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);

        // Act
        var response = await adminClient.DeleteAsync($"/api/pm/projects/{project.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify it's deleted (soft delete - should return not found)
        var getResponse = await adminClient.GetAsync($"/api/pm/projects/{project.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProject_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/pm/projects/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /api/pm/projects/{id}/board

    [Fact]
    public async Task GetKanbanBoard_ValidProjectId_ShouldReturnBoard()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);

        // Act
        var response = await adminClient.GetAsync($"/api/pm/projects/{project.Id}/board");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<KanbanBoardDto>();
        result.ShouldNotBeNull();
        result!.ProjectId.ShouldBe(project.Id);
    }

    [Fact]
    public async Task GetKanbanBoard_InvalidProjectId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/pm/projects/{Guid.NewGuid()}/board");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Labels - POST/PUT/DELETE /api/pm/projects/{id}/labels

    [Fact]
    public async Task GetProjectLabels_ValidProjectId_ShouldReturnLabels()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);

        // Act
        var response = await adminClient.GetAsync($"/api/pm/projects/{project.Id}/labels");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<List<TaskLabelDto>>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateTaskLabel_ValidRequest_ShouldReturnCreatedLabel()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var request = new CreateTaskLabelRequest("Bug", "#EF4444");

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync($"/api/pm/projects/{project.Id}/labels", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var label = await response.Content.ReadFromJsonWithEnumsAsync<TaskLabelDto>();
        label.ShouldNotBeNull();
        label!.Name.ShouldBe("Bug");
        label.Color.ShouldBe("#EF4444");
    }

    [Fact]
    public async Task UpdateTaskLabel_ValidRequest_ShouldReturnUpdatedLabel()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var label = await CreateTestLabelAsync(adminClient, project.Id);

        var updateRequest = new UpdateTaskLabelRequest("Feature", "#22C55E");

        // Act
        var response = await adminClient.PutAsJsonWithEnumsAsync(
            $"/api/pm/projects/{project.Id}/labels/{label.Id}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonWithEnumsAsync<TaskLabelDto>();
        updated.ShouldNotBeNull();
        updated!.Name.ShouldBe("Feature");
    }

    [Fact]
    public async Task DeleteTaskLabel_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var label = await CreateTestLabelAsync(adminClient, project.Id);

        // Act
        var response = await adminClient.DeleteAsync(
            $"/api/pm/projects/{project.Id}/labels/{label.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Columns - POST/PUT/DELETE /api/pm/projects/{id}/columns

    [Fact]
    public async Task CreateColumn_ValidRequest_ShouldReturnCreatedColumn()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var uniqueName = $"Review {Guid.NewGuid().ToString("N")[..6]}";
        var request = new CreateColumnRequest(uniqueName, "#F59E0B", WipLimit: 5);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/projects/{project.Id}/columns", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var column = await response.Content.ReadFromJsonWithEnumsAsync<ProjectColumnDto>();
        column.ShouldNotBeNull();
        column!.Name.ShouldBe(uniqueName);
    }

    [Fact]
    public async Task UpdateColumn_ValidRequest_ShouldReturnUpdatedColumn()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);

        // Create a column first
        var createRequest = new CreateColumnRequest("Testing", "#A855F7");
        var createResponse = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/projects/{project.Id}/columns", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var column = await createResponse.Content.ReadFromJsonWithEnumsAsync<ProjectColumnDto>();

        var updateRequest = new UpdateColumnRequest("QA Testing", "#7C3AED", WipLimit: 3);

        // Act
        var response = await adminClient.PutAsJsonWithEnumsAsync(
            $"/api/pm/projects/{project.Id}/columns/{column!.Id}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonWithEnumsAsync<ProjectColumnDto>();
        updated.ShouldNotBeNull();
        updated!.Name.ShouldBe("QA Testing");
    }

    #endregion

    #region Members - GET/POST/DELETE/PUT /api/pm/projects/{id}/members

    [Fact]
    public async Task GetProjectMembers_ValidProjectId_ShouldReturnMembers()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);

        // Act
        var response = await adminClient.GetAsync($"/api/pm/projects/{project.Id}/members");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<List<ProjectMemberDto>>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task AddProjectMember_ValidRequest_ShouldReturnMember()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var employee = await CreateTestEmployeeAsync(adminClient);

        var request = new AddProjectMemberRequest(employee.Id, ProjectMemberRole.Member);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/projects/{project.Id}/members", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var member = await response.Content.ReadFromJsonWithEnumsAsync<ProjectMemberDto>();
        member.ShouldNotBeNull();
        member!.EmployeeId.ShouldBe(employee.Id);
        member.Role.ShouldBe(ProjectMemberRole.Member);
    }

    [Fact]
    public async Task AddProjectMember_InvalidProjectId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new AddProjectMemberRequest(Guid.NewGuid(), ProjectMemberRole.Member);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/projects/{Guid.NewGuid()}/members", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveProjectMember_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var employee = await CreateTestEmployeeAsync(adminClient);

        // Add member first
        var addRequest = new AddProjectMemberRequest(employee.Id, ProjectMemberRole.Member);
        var addResponse = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/projects/{project.Id}/members", addRequest);
        addResponse.EnsureSuccessStatusCode();
        var member = await addResponse.Content.ReadFromJsonWithEnumsAsync<ProjectMemberDto>();

        // Act
        var response = await adminClient.DeleteAsync(
            $"/api/pm/projects/{project.Id}/members/{member!.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangeProjectMemberRole_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var employee = await CreateTestEmployeeAsync(adminClient);

        // Add member first
        var addRequest = new AddProjectMemberRequest(employee.Id, ProjectMemberRole.Member);
        var addResponse = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/projects/{project.Id}/members", addRequest);
        addResponse.EnsureSuccessStatusCode();
        var member = await addResponse.Content.ReadFromJsonWithEnumsAsync<ProjectMemberDto>();

        var roleRequest = new ChangeProjectMemberRoleRequest(ProjectMemberRole.Manager);

        // Act
        var response = await adminClient.PutAsJsonWithEnumsAsync(
            $"/api/pm/projects/{project.Id}/members/{member!.Id}/role", roleRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonWithEnumsAsync<ProjectMemberDto>();
        updated.ShouldNotBeNull();
        updated!.Role.ShouldBe(ProjectMemberRole.Manager);
    }

    #endregion

    #region Columns - POST reorder + DELETE /api/pm/projects/{id}/columns

    [Fact]
    public async Task ReorderColumns_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);

        // Get board to obtain existing column IDs
        var boardResponse = await adminClient.GetAsync($"/api/pm/projects/{project.Id}/board");
        boardResponse.EnsureSuccessStatusCode();
        var board = await boardResponse.Content.ReadFromJsonWithEnumsAsync<KanbanBoardDto>();
        var columnIds = board!.Columns.Select(c => c.Id).Reverse().ToList();

        var request = new ReorderColumnsRequest(columnIds);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/projects/{project.Id}/columns/reorder", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<List<ProjectColumnDto>>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteColumn_WithNoTasks_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);

        // Get board to find an existing column to use as move target
        var boardResponse = await adminClient.GetAsync($"/api/pm/projects/{project.Id}/board");
        boardResponse.EnsureSuccessStatusCode();
        var board = await boardResponse.Content.ReadFromJsonWithEnumsAsync<KanbanBoardDto>();
        var targetColumnId = board!.Columns.First().Id;

        // Create a new column to delete
        var uniqueName = $"Deletable {Guid.NewGuid().ToString("N")[..6]}";
        var createRequest = new CreateColumnRequest(uniqueName, "#FF0000");
        var createResponse = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/projects/{project.Id}/columns", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var column = await createResponse.Content.ReadFromJsonWithEnumsAsync<ProjectColumnDto>();

        // Act — DeleteColumn uses query param: ?moveToColumnId=...
        var response = await adminClient.DeleteAsync(
            $"/api/pm/projects/{project.Id}/columns/{column!.Id}?moveToColumnId={targetColumnId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteColumn_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);

        // Act
        var response = await adminClient.DeleteAsync(
            $"/api/pm/projects/{project.Id}/columns/{Guid.NewGuid()}?moveToColumnId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region ChangeProjectStatus state machine

    [Fact]
    public async Task ChangeProjectStatus_ToArchivedFromActive_ShouldSucceed()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var request = new ChangeProjectStatusRequest(ProjectStatus.Archived);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/projects/{project.Id}/status", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<ProjectDto>();
        result.ShouldNotBeNull();
        result!.Status.ShouldBe(ProjectStatus.Archived);
    }

    [Fact]
    public async Task ChangeProjectStatus_SameStatus_ShouldReturnBadRequest()
    {
        // Arrange — project starts Active; try to set Active again
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var request = new ChangeProjectStatusRequest(ProjectStatus.Active);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/projects/{project.Id}/status", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region CRUD Lifecycle

    [Fact]
    public async Task Project_FullCrudLifecycle_ShouldSucceed()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Create
        var createRequest = CreateTestProjectRequest();
        var createResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/pm/projects", createRequest);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonWithEnumsAsync<ProjectDto>();
        created.ShouldNotBeNull();

        // Read
        var getResponse = await adminClient.GetAsync($"/api/pm/projects/{created!.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Update
        var updateRequest = new UpdateProjectRequest(
            "Lifecycle Updated Project",
            Description: "Updated via lifecycle test");
        var updateResponse = await adminClient.PutAsJsonWithEnumsAsync($"/api/pm/projects/{created.Id}", updateRequest);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonWithEnumsAsync<ProjectDto>();
        updated!.Name.ShouldBe("Lifecycle Updated Project");

        // Delete
        var deleteResponse = await adminClient.DeleteAsync($"/api/pm/projects/{created.Id}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region DI Verification (Rule #21)

    [Fact]
    public async Task IRepository_Project_ShouldResolveFromDI()
    {
        await _factory.ExecuteWithTenantAsync(sp =>
        {
            var repository = sp.GetRequiredService<IRepository<Project, Guid>>();
            repository.ShouldNotBeNull();
            return Task.CompletedTask;
        });
    }

    #endregion

    #region Helper Methods

    private async Task<ProjectDto> CreateTestProjectAsync(HttpClient adminClient)
    {
        var request = CreateTestProjectRequest();
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/pm/projects", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonWithEnumsAsync<ProjectDto>())!;
    }

    private async Task<TaskLabelDto> CreateTestLabelAsync(HttpClient adminClient, Guid projectId)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var request = new CreateTaskLabelRequest($"Label {uniqueId}", "#3B82F6");
        var response = await adminClient.PostAsJsonWithEnumsAsync($"/api/pm/projects/{projectId}/labels", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonWithEnumsAsync<TaskLabelDto>())!;
    }

    private static CreateProjectRequest CreateTestProjectRequest()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return new CreateProjectRequest(
            $"Test Project {uniqueId}",
            Description: "Integration test project",
            Visibility: ProjectVisibility.Internal);
    }

    private async Task<EmployeeDto> CreateTestEmployeeAsync(HttpClient adminClient)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var deptRequest = new CreateDepartmentRequest(
            $"PM Dept {uniqueId}", $"PMD-{uniqueId}");
        var deptResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/departments", deptRequest);
        deptResponse.EnsureSuccessStatusCode();
        var dept = (await deptResponse.Content.ReadFromJsonWithEnumsAsync<DepartmentDto>())!;

        var empRequest = new CreateEmployeeRequest(
            $"PmTest{uniqueId}", $"Employee{uniqueId}",
            $"pm-emp-{uniqueId}@test.com",
            dept.Id, DateTimeOffset.UtcNow, EmploymentType.FullTime);
        var empResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/employees", empRequest);
        empResponse.EnsureSuccessStatusCode();
        return (await empResponse.Content.ReadFromJsonWithEnumsAsync<EmployeeDto>())!;
    }

    #endregion
}
