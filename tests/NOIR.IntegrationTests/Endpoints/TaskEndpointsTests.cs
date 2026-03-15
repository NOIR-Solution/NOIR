using NOIR.Application.Features.Hr.DTOs;
using NOIR.Application.Features.Pm.DTOs;
using NOIR.Domain.Entities.Pm;
using NOIR.Domain.Enums;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for project task management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class TaskEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TaskEndpointsTests(CustomWebApplicationFactory factory)
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

    #region GET /api/pm/tasks

    [Fact]
    public async Task GetTasks_AsAdmin_WithProjectId_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);

        // Act
        var response = await adminClient.GetAsync($"/api/pm/tasks?projectId={project.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<TaskCardDto>>();
        result.ShouldNotBeNull();
        result!.Items.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetTasks_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/pm/tasks?projectId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTasks_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);

        // Act
        var response = await adminClient.GetAsync($"/api/pm/tasks?projectId={project.Id}&page=1&pageSize=5");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<TaskCardDto>>();
        result.ShouldNotBeNull();
        result!.Items.Count.ShouldBeLessThanOrEqualTo(5);
    }

    #endregion

    #region GET /api/pm/tasks/search

    [Fact]
    public async Task SearchTasks_AsAdmin_ShouldReturnResults()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);

        // Act
        var response = await adminClient.GetAsync($"/api/pm/tasks/search?projectId={project.Id}&q=test");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<List<TaskSearchDto>>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task SearchTasks_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/pm/tasks/search?projectId={Guid.NewGuid()}&q=test");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/pm/tasks/{id}

    [Fact]
    public async Task GetTaskById_ValidId_ShouldReturnTask()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var task = await CreateTestTaskAsync(adminClient, project.Id);

        // Act
        var response = await adminClient.GetAsync($"/api/pm/tasks/{task.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<TaskDto>();
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(task.Id);
        result.Title.ShouldBe(task.Title);
    }

    [Fact]
    public async Task GetTaskById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/pm/tasks/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTaskById_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/pm/tasks/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/pm/tasks

    [Fact]
    public async Task CreateTask_ValidRequest_ShouldReturnCreatedTask()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var request = CreateTestTaskRequest(project.Id);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/pm/tasks", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var task = await response.Content.ReadFromJsonWithEnumsAsync<TaskDto>();
        task.ShouldNotBeNull();
        task!.Title.ShouldBe(request.Title);
        task.ProjectId.ShouldBe(project.Id);
        task.TaskNumber.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateTask_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new CreateTaskRequest(Guid.NewGuid(), "Test Task");

        // Act
        var response = await _client.PostAsJsonWithEnumsAsync("/api/pm/tasks", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTask_InvalidProjectId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new CreateTaskRequest(Guid.NewGuid(), "Test Task");

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/pm/tasks", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region PUT /api/pm/tasks/{id}

    [Fact]
    public async Task UpdateTask_ValidRequest_ShouldReturnUpdatedTask()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var task = await CreateTestTaskAsync(adminClient, project.Id);

        var updateRequest = new UpdateTaskRequest(
            Title: "Updated Task Title",
            Description: "Updated description",
            Priority: TaskPriority.High);

        // Act
        var response = await adminClient.PutAsJsonWithEnumsAsync($"/api/pm/tasks/{task.Id}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonWithEnumsAsync<TaskDto>();
        updated.ShouldNotBeNull();
        updated!.Title.ShouldBe("Updated Task Title");
        updated.Priority.ShouldBe(TaskPriority.High);
    }

    [Fact]
    public async Task UpdateTask_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var updateRequest = new UpdateTaskRequest(Title: "Updated");

        // Act
        var response = await adminClient.PutAsJsonWithEnumsAsync($"/api/pm/tasks/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/pm/tasks/{id}/status

    [Fact]
    public async Task ChangeTaskStatus_ValidRequest_ShouldReturnUpdatedTask()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var task = await CreateTestTaskAsync(adminClient, project.Id);

        var statusRequest = new ChangeTaskStatusRequest(ProjectTaskStatus.InProgress);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync($"/api/pm/tasks/{task.Id}/status", statusRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<TaskDto>();
        result.ShouldNotBeNull();
        result!.Status.ShouldBe(ProjectTaskStatus.InProgress);
    }

    [Fact]
    public async Task ChangeTaskStatus_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var statusRequest = new ChangeTaskStatusRequest(ProjectTaskStatus.Done);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync($"/api/pm/tasks/{Guid.NewGuid()}/status", statusRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/pm/tasks/{id}/subtasks

    [Fact]
    public async Task AddSubtask_ValidRequest_ShouldReturnParentTask()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var parentTask = await CreateTestTaskAsync(adminClient, project.Id);

        var request = new AddSubtaskRequest("Subtask Title", Priority: TaskPriority.Low);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync($"/api/pm/tasks/{parentTask.Id}/subtasks", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<TaskDto>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task AddSubtask_InvalidParentId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new AddSubtaskRequest("Subtask Title");

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync($"/api/pm/tasks/{Guid.NewGuid()}/subtasks", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/pm/tasks/{id}

    [Fact]
    public async Task DeleteTask_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var task = await CreateTestTaskAsync(adminClient, project.Id);

        // Act
        var response = await adminClient.DeleteAsync($"/api/pm/tasks/{task.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify it's deleted (soft delete - should return not found)
        var getResponse = await adminClient.GetAsync($"/api/pm/tasks/{task.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTask_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/pm/tasks/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Comments - POST/PUT/DELETE /api/pm/tasks/{id}/comments

    [Fact]
    public async Task AddTaskComment_ValidRequest_ShouldReturnComment()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        await EnsureAdminHasLinkedEmployeeAsync(adminClient);
        var project = await CreateTestProjectAsync(adminClient);
        var task = await CreateTestTaskAsync(adminClient, project.Id);

        var request = new AddTaskCommentRequest("This is a test comment.");

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync($"/api/pm/tasks/{task.Id}/comments", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var comment = await response.Content.ReadFromJsonWithEnumsAsync<TaskCommentDto>();
        comment.ShouldNotBeNull();
        comment!.Content.ShouldBe("This is a test comment.");
    }

    [Fact]
    public async Task AddTaskComment_InvalidTaskId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new AddTaskCommentRequest("Comment on non-existent task.");

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync($"/api/pm/tasks/{Guid.NewGuid()}/comments", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTaskComment_ValidRequest_ShouldReturnUpdatedComment()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        await EnsureAdminHasLinkedEmployeeAsync(adminClient);
        var project = await CreateTestProjectAsync(adminClient);
        var task = await CreateTestTaskAsync(adminClient, project.Id);

        // Add a comment first
        var addRequest = new AddTaskCommentRequest("Original comment.");
        var addResponse = await adminClient.PostAsJsonWithEnumsAsync($"/api/pm/tasks/{task.Id}/comments", addRequest);
        addResponse.EnsureSuccessStatusCode();
        var comment = await addResponse.Content.ReadFromJsonWithEnumsAsync<TaskCommentDto>();

        var updateRequest = new UpdateTaskCommentRequest("Updated comment content.");

        // Act
        var response = await adminClient.PutAsJsonWithEnumsAsync(
            $"/api/pm/tasks/{task.Id}/comments/{comment!.Id}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonWithEnumsAsync<TaskCommentDto>();
        updated.ShouldNotBeNull();
        updated!.Content.ShouldBe("Updated comment content.");
    }

    [Fact]
    public async Task DeleteTaskComment_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        await EnsureAdminHasLinkedEmployeeAsync(adminClient);
        var project = await CreateTestProjectAsync(adminClient);
        var task = await CreateTestTaskAsync(adminClient, project.Id);

        // Add a comment first
        var addRequest = new AddTaskCommentRequest("Comment to delete.");
        var addResponse = await adminClient.PostAsJsonWithEnumsAsync($"/api/pm/tasks/{task.Id}/comments", addRequest);
        addResponse.EnsureSuccessStatusCode();
        var comment = await addResponse.Content.ReadFromJsonWithEnumsAsync<TaskCommentDto>();

        // Act
        var response = await adminClient.DeleteAsync(
            $"/api/pm/tasks/{task.Id}/comments/{comment!.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Task Labels - POST/DELETE /api/pm/tasks/{id}/labels/{labelId}

    [Fact]
    public async Task AddLabelToTask_ValidRequest_ShouldReturnLabel()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var task = await CreateTestTaskAsync(adminClient, project.Id);
        var label = await CreateTestLabelAsync(adminClient, project.Id);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/tasks/{task.Id}/labels/{label.Id}", new { });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<TaskLabelBriefDto>();
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(label.Id);
    }

    [Fact]
    public async Task RemoveLabelFromTask_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var task = await CreateTestTaskAsync(adminClient, project.Id);
        var label = await CreateTestLabelAsync(adminClient, project.Id);

        // First add label to task
        await adminClient.PostAsJsonWithEnumsAsync($"/api/pm/tasks/{task.Id}/labels/{label.Id}", new { });

        // Act - remove label
        var response = await adminClient.DeleteAsync(
            $"/api/pm/tasks/{task.Id}/labels/{label.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region POST /api/pm/tasks/{id}/move

    [Fact]
    public async Task MoveTask_ValidRequest_ShouldReturnUpdatedTask()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var task = await CreateTestTaskAsync(adminClient, project.Id);

        // Get board to find a different column to move to
        var boardResponse = await adminClient.GetAsync($"/api/pm/projects/{project.Id}/board");
        boardResponse.EnsureSuccessStatusCode();
        var board = await boardResponse.Content.ReadFromJsonWithEnumsAsync<KanbanBoardDto>();

        // Pick a column different from the task's current column (or second column)
        var targetColumn = board!.Columns.Count > 1 ? board.Columns[1] : board.Columns[0];
        var request = new MoveTaskRequest(targetColumn.Id, 1.0);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/tasks/{task.Id}/move", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<TaskDto>();
        result.ShouldNotBeNull();
        result!.ColumnId.ShouldBe(targetColumn.Id);
    }

    [Fact]
    public async Task MoveTask_InvalidTaskId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new MoveTaskRequest(Guid.NewGuid(), 1.0);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/tasks/{Guid.NewGuid()}/move", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/pm/tasks/{id}/reorder

    [Fact]
    public async Task ReorderTask_ValidRequest_ShouldReturnUpdatedTask()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var task = await CreateTestTaskAsync(adminClient, project.Id);

        var request = new ReorderTaskRequest(100.5);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/tasks/{task.Id}/reorder", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<TaskDto>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReorderTask_InvalidTaskId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new ReorderTaskRequest(1.0);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/tasks/{Guid.NewGuid()}/reorder", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region ChangeTaskStatus state machine

    [Fact]
    public async Task ChangeTaskStatus_AlreadyDone_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var task = await CreateTestTaskAsync(adminClient, project.Id);

        // Set task to Done first
        var doneRequest = new ChangeTaskStatusRequest(ProjectTaskStatus.Done);
        var doneResponse = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/tasks/{task.Id}/status", doneRequest);
        doneResponse.EnsureSuccessStatusCode();

        // Act — try to set Done again
        var response = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/tasks/{task.Id}/status", doneRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangeTaskStatus_FromDoneToInProgress_ShouldReturnUpdatedTask()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);
        var task = await CreateTestTaskAsync(adminClient, project.Id);

        // Set task to Done first
        var doneRequest = new ChangeTaskStatusRequest(ProjectTaskStatus.Done);
        var doneResponse = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/tasks/{task.Id}/status", doneRequest);
        doneResponse.EnsureSuccessStatusCode();

        // Act — set back to InProgress (valid transition)
        var inProgressRequest = new ChangeTaskStatusRequest(ProjectTaskStatus.InProgress);
        var response = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/tasks/{task.Id}/status", inProgressRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<TaskDto>();
        result.ShouldNotBeNull();
        result!.Status.ShouldBe(ProjectTaskStatus.InProgress);
    }

    #endregion

    #region CRUD Lifecycle

    [Fact]
    public async Task Task_FullCrudLifecycle_ShouldSucceed()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var project = await CreateTestProjectAsync(adminClient);

        // Create
        var createRequest = CreateTestTaskRequest(project.Id);
        var createResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/pm/tasks", createRequest);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonWithEnumsAsync<TaskDto>();
        created.ShouldNotBeNull();

        // Read
        var getResponse = await adminClient.GetAsync($"/api/pm/tasks/{created!.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Update
        var updateRequest = new UpdateTaskRequest(
            Title: "Lifecycle Updated Task",
            Description: "Updated via lifecycle test",
            Priority: TaskPriority.Urgent);
        var updateResponse = await adminClient.PutAsJsonWithEnumsAsync($"/api/pm/tasks/{created.Id}", updateRequest);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonWithEnumsAsync<TaskDto>();
        updated!.Title.ShouldBe("Lifecycle Updated Task");

        // Change status
        var statusResponse = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/pm/tasks/{created.Id}/status",
            new ChangeTaskStatusRequest(ProjectTaskStatus.InProgress));
        statusResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Delete
        var deleteResponse = await adminClient.DeleteAsync($"/api/pm/tasks/{created.Id}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region DI Verification (Rule #21)

    [Fact]
    public async Task IRepository_ProjectTask_ShouldResolveFromDI()
    {
        await _factory.ExecuteWithTenantAsync(sp =>
        {
            var repository = sp.GetRequiredService<IRepository<ProjectTask, Guid>>();
            repository.ShouldNotBeNull();
            return Task.CompletedTask;
        });
    }

    #endregion

    #region Helper Methods

    private async Task<ProjectDto> CreateTestProjectAsync(HttpClient adminClient)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var request = new CreateProjectRequest(
            $"Task Test Project {uniqueId}",
            Description: "Integration test project for tasks",
            Visibility: ProjectVisibility.Internal);
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/pm/projects", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonWithEnumsAsync<ProjectDto>())!;
    }

    private async Task<TaskDto> CreateTestTaskAsync(HttpClient adminClient, Guid projectId)
    {
        var request = CreateTestTaskRequest(projectId);
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/pm/tasks", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonWithEnumsAsync<TaskDto>())!;
    }

    private async Task<TaskLabelDto> CreateTestLabelAsync(HttpClient adminClient, Guid projectId)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var request = new CreateTaskLabelRequest($"Label {uniqueId}", "#3B82F6");
        var response = await adminClient.PostAsJsonWithEnumsAsync($"/api/pm/projects/{projectId}/labels", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonWithEnumsAsync<TaskLabelDto>())!;
    }

    private async Task EnsureAdminHasLinkedEmployeeAsync(HttpClient adminClient)
    {
        // Login to get the admin user's UserId
        var loginCommand = new LoginCommand("admin@noir.local", "123qwe");
        var loginResponse = await _client.PostAsJsonWithEnumsAsync("/api/auth/login", loginCommand);
        var login = await loginResponse.Content.ReadFromJsonWithEnumsAsync<LoginResponse>();
        var adminUserId = login!.Auth!.UserId;

        // Create a department and employee, then link to the admin user
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var deptRequest = new CreateDepartmentRequest(
            $"Comment Dept {uniqueId}", $"CMD-{uniqueId}");
        var deptResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/departments", deptRequest);
        deptResponse.EnsureSuccessStatusCode();
        var dept = (await deptResponse.Content.ReadFromJsonWithEnumsAsync<DepartmentDto>())!;

        var empRequest = new CreateEmployeeRequest(
            $"Admin{uniqueId}", $"Emp{uniqueId}", $"admin-emp-{uniqueId}@test.com",
            dept.Id, DateTimeOffset.UtcNow, EmploymentType.FullTime);
        var empResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/employees", empRequest);
        empResponse.EnsureSuccessStatusCode();
        var employee = (await empResponse.Content.ReadFromJsonWithEnumsAsync<EmployeeDto>())!;

        // Link the employee to the admin user
        var linkRequest = new { TargetUserId = adminUserId };
        var linkResponse = await adminClient.PostAsJsonWithEnumsAsync(
            $"/api/hr/employees/{employee.Id}/link-user", linkRequest);
        // If it fails (e.g., already linked), that's fine - the employee is still linked
    }

    private static CreateTaskRequest CreateTestTaskRequest(Guid projectId)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return new CreateTaskRequest(
            projectId,
            $"Test Task {uniqueId}",
            Description: "Integration test task",
            Priority: TaskPriority.Medium);
    }

    #endregion
}
