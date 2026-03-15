using NOIR.Application.Features.Crm.DTOs;
using NOIR.Application.Features.Hr.DTOs;
using NOIR.Domain.Enums;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for CRM activity management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class CrmActivityEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CrmActivityEndpointsTests(CustomWebApplicationFactory factory)
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

    private async Task<Guid> GetOrCreateTestEmployeeIdAsync(HttpClient adminClient)
    {
        // Create a department first, then an employee to use as PerformedById
        // PerformedById references Employees table (FK_CrmActivities_Employees_PerformedById)
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var deptRequest = new CreateDepartmentRequest(
            $"Act Dept {uniqueId}", $"ACD-{uniqueId}");
        var deptResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/departments", deptRequest);
        deptResponse.EnsureSuccessStatusCode();
        var dept = (await deptResponse.Content.ReadFromJsonWithEnumsAsync<DepartmentDto>())!;

        var empRequest = new CreateEmployeeRequest(
            $"Perf{uniqueId}", $"By{uniqueId}", $"perf-{uniqueId}@test.com",
            dept.Id, DateTimeOffset.UtcNow, EmploymentType.FullTime);
        var empResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/hr/employees", empRequest);
        empResponse.EnsureSuccessStatusCode();
        var employee = (await empResponse.Content.ReadFromJsonWithEnumsAsync<EmployeeDto>())!;
        return employee.Id;
    }

    private async Task<ContactDto> CreateTestContactAsync(HttpClient adminClient)
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        var request = new CreateContactRequest(
            FirstName: $"Act-{uniqueId[..6]}",
            LastName: $"Test-{uniqueId[6..12]}",
            Email: $"activity-{uniqueId}@test.com",
            Source: ContactSource.Web);
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/crm/contacts", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonWithEnumsAsync<ContactDto>())!;
    }

    #region GET /api/crm/activities

    [Fact]
    public async Task GetActivities_AsAdmin_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/crm/activities");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<ActivityDto>>();
        result.ShouldNotBeNull();
        result!.Items.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetActivities_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/crm/activities");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetActivities_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/crm/activities?page=1&pageSize=5");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<ActivityDto>>();
        result.ShouldNotBeNull();
        result!.Items.Count.ShouldBeLessThanOrEqualTo(5);
    }

    #endregion

    #region GET /api/crm/activities/{id}

    [Fact]
    public async Task GetActivityById_ValidId_ShouldReturnActivity()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var userId = await GetOrCreateTestEmployeeIdAsync(adminClient);
        var contact = await CreateTestContactAsync(adminClient);
        var createRequest = CreateTestActivityRequest(userId, contact.Id);
        var createResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/crm/activities", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdActivity = await createResponse.Content.ReadFromJsonWithEnumsAsync<ActivityDto>();

        // Act
        var response = await adminClient.GetAsync($"/api/crm/activities/{createdActivity!.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var activity = await response.Content.ReadFromJsonWithEnumsAsync<ActivityDto>();
        activity.ShouldNotBeNull();
        activity!.Id.ShouldBe(createdActivity.Id);
        activity.Subject.ShouldBe(createRequest.Subject);
        activity.Type.ShouldBe(createRequest.Type);
    }

    [Fact]
    public async Task GetActivityById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/crm/activities/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/crm/activities

    [Fact]
    public async Task CreateActivity_ValidRequest_ShouldReturnCreatedActivity()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var userId = await GetOrCreateTestEmployeeIdAsync(adminClient);
        var contact = await CreateTestContactAsync(adminClient);
        var request = CreateTestActivityRequest(userId, contact.Id);

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/crm/activities", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var activity = await response.Content.ReadFromJsonWithEnumsAsync<ActivityDto>();
        activity.ShouldNotBeNull();
        activity!.Subject.ShouldBe(request.Subject);
        activity.Type.ShouldBe(ActivityType.Call);
    }

    [Fact]
    public async Task CreateActivity_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = CreateTestActivityRequest(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var response = await _client.PostAsJsonWithEnumsAsync("/api/crm/activities", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT /api/crm/activities/{id}

    [Fact]
    public async Task UpdateActivity_ValidRequest_ShouldReturnUpdatedActivity()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var userId = await GetOrCreateTestEmployeeIdAsync(adminClient);
        var contact = await CreateTestContactAsync(adminClient);
        var createRequest = CreateTestActivityRequest(userId, contact.Id);
        var createResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/crm/activities", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdActivity = await createResponse.Content.ReadFromJsonWithEnumsAsync<ActivityDto>();

        var updateRequest = new UpdateActivityRequest(
            Type: ActivityType.Meeting,
            Subject: "Updated Meeting Subject",
            PerformedAt: DateTimeOffset.UtcNow,
            Description: "Updated description",
            ContactId: contact.Id,
            DurationMinutes: 60);

        // Act
        var response = await adminClient.PutAsJsonWithEnumsAsync($"/api/crm/activities/{createdActivity!.Id}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updatedActivity = await response.Content.ReadFromJsonWithEnumsAsync<ActivityDto>();
        updatedActivity.ShouldNotBeNull();
        updatedActivity!.Subject.ShouldBe("Updated Meeting Subject");
        updatedActivity.Type.ShouldBe(ActivityType.Meeting);
        updatedActivity.DurationMinutes.ShouldBe(60);
    }

    [Fact]
    public async Task UpdateActivity_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var updateRequest = new UpdateActivityRequest(
            Type: ActivityType.Call,
            Subject: "Test",
            PerformedAt: DateTimeOffset.UtcNow);

        // Act
        var response = await adminClient.PutAsJsonWithEnumsAsync($"/api/crm/activities/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/crm/activities/{id}

    [Fact]
    public async Task DeleteActivity_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var userId = await GetOrCreateTestEmployeeIdAsync(adminClient);
        var contact = await CreateTestContactAsync(adminClient);
        var createRequest = CreateTestActivityRequest(userId, contact.Id);
        var createResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/crm/activities", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdActivity = await createResponse.Content.ReadFromJsonWithEnumsAsync<ActivityDto>();

        // Act
        var response = await adminClient.DeleteAsync($"/api/crm/activities/{createdActivity!.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify it's deleted (soft delete - should return not found)
        var getResponse = await adminClient.GetAsync($"/api/crm/activities/{createdActivity.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteActivity_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/crm/activities/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Full CRUD Cycle

    [Fact]
    public async Task Activity_FullCrudCycle_ShouldSucceed()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var userId = await GetOrCreateTestEmployeeIdAsync(adminClient);
        var contact = await CreateTestContactAsync(adminClient);

        // Create
        var createRequest = CreateTestActivityRequest(userId, contact.Id);
        var createResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/crm/activities", createRequest);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonWithEnumsAsync<ActivityDto>();
        created.ShouldNotBeNull();
        var activityId = created!.Id;

        // Read
        var getResponse = await adminClient.GetAsync($"/api/crm/activities/{activityId}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonWithEnumsAsync<ActivityDto>();
        fetched!.Subject.ShouldBe(createRequest.Subject);

        // Update
        var updateRequest = new UpdateActivityRequest(
            Type: ActivityType.Email,
            Subject: "CrudUpdated Activity",
            PerformedAt: DateTimeOffset.UtcNow,
            ContactId: contact.Id,
            Description: "Updated via CRUD test");
        var updateResponse = await adminClient.PutAsJsonWithEnumsAsync($"/api/crm/activities/{activityId}", updateRequest);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonWithEnumsAsync<ActivityDto>();
        updated!.Subject.ShouldBe("CrudUpdated Activity");
        updated.Type.ShouldBe(ActivityType.Email);

        // Delete
        var deleteResponse = await adminClient.DeleteAsync($"/api/crm/activities/{activityId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify deleted
        var verifyResponse = await adminClient.GetAsync($"/api/crm/activities/{activityId}");
        verifyResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Filter by Contact

    [Fact]
    public async Task GetActivities_FilterByContactId_ShouldReturnFilteredResults()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var userId = await GetOrCreateTestEmployeeIdAsync(adminClient);
        var contact = await CreateTestContactAsync(adminClient);
        var request = CreateTestActivityRequest(userId, contact.Id);
        var createResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/crm/activities", request);
        createResponse.EnsureSuccessStatusCode();

        // Act
        var response = await adminClient.GetAsync($"/api/crm/activities?contactId={contact.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<ActivityDto>>();
        result.ShouldNotBeNull();
        result!.Items.ShouldAllBe(a => a.ContactId == contact.Id);
    }

    #endregion

    #region Helper Methods

    private static CreateActivityRequest CreateTestActivityRequest(Guid performedById, Guid contactId)
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        return new CreateActivityRequest(
            Type: ActivityType.Call,
            Subject: $"Test Call {uniqueId[..8]}",
            PerformedById: performedById,
            PerformedAt: DateTimeOffset.UtcNow,
            Description: "Integration test activity",
            ContactId: contactId,
            DurationMinutes: 30);
    }

    #endregion
}
