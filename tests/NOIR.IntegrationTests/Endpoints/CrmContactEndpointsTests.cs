using NOIR.Application.Features.Crm.DTOs;
using NOIR.Domain.Enums;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for CRM contact management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class CrmContactEndpointsTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CrmContactEndpointsTests(CustomWebApplicationFactory factory)
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

    #region GET /api/crm/contacts

    [Fact]
    public async Task GetContacts_AsAdmin_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/crm/contacts");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<ContactListDto>>();
        result.ShouldNotBeNull();
        result!.Items.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetContacts_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/crm/contacts");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetContacts_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/crm/contacts?page=1&pageSize=5");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<ContactListDto>>();
        result.ShouldNotBeNull();
        result!.Items.Count.ShouldBeLessThanOrEqualTo(5);
    }

    #endregion

    #region GET /api/crm/contacts/{id}

    [Fact]
    public async Task GetContactById_ValidId_ShouldReturnContact()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var createRequest = CreateTestContactRequest();
        var createResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/crm/contacts", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdContact = await createResponse.Content.ReadFromJsonWithEnumsAsync<ContactDto>();

        // Act
        var response = await adminClient.GetAsync($"/api/crm/contacts/{createdContact!.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var contact = await response.Content.ReadFromJsonWithEnumsAsync<ContactDto>();
        contact.ShouldNotBeNull();
        contact!.Id.ShouldBe(createdContact.Id);
        contact.FirstName.ShouldBe(createRequest.FirstName);
        contact.LastName.ShouldBe(createRequest.LastName);
        contact.Email.ShouldBe(createRequest.Email);
    }

    [Fact]
    public async Task GetContactById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/crm/contacts/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/crm/contacts

    [Fact]
    public async Task CreateContact_ValidRequest_ShouldReturnCreatedContact()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestContactRequest();

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/crm/contacts", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var contact = await response.Content.ReadFromJsonWithEnumsAsync<ContactDto>();
        contact.ShouldNotBeNull();
        contact!.FirstName.ShouldBe(request.FirstName);
        contact.LastName.ShouldBe(request.LastName);
        contact.Email.ShouldBe(request.Email);
        contact.Source.ShouldBe(ContactSource.Web);
    }

    [Fact]
    public async Task CreateContact_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = CreateTestContactRequest();

        // Act
        var response = await _client.PostAsJsonWithEnumsAsync("/api/crm/contacts", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT /api/crm/contacts/{id}

    [Fact]
    public async Task UpdateContact_ValidRequest_ShouldReturnUpdatedContact()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var createRequest = CreateTestContactRequest();
        var createResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/crm/contacts", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdContact = await createResponse.Content.ReadFromJsonWithEnumsAsync<ContactDto>();

        var updateRequest = new UpdateContactRequest(
            FirstName: "UpdatedFirst",
            LastName: "UpdatedLast",
            Email: $"updated-{Guid.NewGuid():N}@test.com",
            Source: ContactSource.Referral,
            Phone: "+1234567890",
            JobTitle: "Updated Title");

        // Act
        var response = await adminClient.PutAsJsonWithEnumsAsync($"/api/crm/contacts/{createdContact!.Id}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updatedContact = await response.Content.ReadFromJsonWithEnumsAsync<ContactDto>();
        updatedContact.ShouldNotBeNull();
        updatedContact!.FirstName.ShouldBe("UpdatedFirst");
        updatedContact.LastName.ShouldBe("UpdatedLast");
        updatedContact.Source.ShouldBe(ContactSource.Referral);
    }

    [Fact]
    public async Task UpdateContact_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var updateRequest = new UpdateContactRequest(
            FirstName: "Test",
            LastName: "Contact",
            Email: "test@test.com",
            Source: ContactSource.Web);

        // Act
        var response = await adminClient.PutAsJsonWithEnumsAsync($"/api/crm/contacts/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/crm/contacts/{id}

    [Fact]
    public async Task DeleteContact_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestContactRequest();
        var createResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/crm/contacts", request);
        createResponse.EnsureSuccessStatusCode();
        var createdContact = await createResponse.Content.ReadFromJsonWithEnumsAsync<ContactDto>();

        // Act
        var response = await adminClient.DeleteAsync($"/api/crm/contacts/{createdContact!.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify it's deleted (soft delete - should return not found)
        var getResponse = await adminClient.GetAsync($"/api/crm/contacts/{createdContact.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteContact_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/crm/contacts/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Full CRUD Cycle

    [Fact]
    public async Task Contact_FullCrudCycle_ShouldSucceed()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Create
        var createRequest = CreateTestContactRequest();
        var createResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/crm/contacts", createRequest);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonWithEnumsAsync<ContactDto>();
        created.ShouldNotBeNull();
        var contactId = created!.Id;

        // Read
        var getResponse = await adminClient.GetAsync($"/api/crm/contacts/{contactId}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonWithEnumsAsync<ContactDto>();
        fetched!.FirstName.ShouldBe(createRequest.FirstName);

        // Update
        var updateRequest = new UpdateContactRequest(
            FirstName: "CrudUpdated",
            LastName: fetched.LastName,
            Email: fetched.Email,
            Source: ContactSource.Social,
            Notes: "Updated via CRUD test");
        var updateResponse = await adminClient.PutAsJsonWithEnumsAsync($"/api/crm/contacts/{contactId}", updateRequest);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonWithEnumsAsync<ContactDto>();
        updated!.FirstName.ShouldBe("CrudUpdated");
        updated.Source.ShouldBe(ContactSource.Social);

        // Delete
        var deleteResponse = await adminClient.DeleteAsync($"/api/crm/contacts/{contactId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify deleted
        var verifyResponse = await adminClient.GetAsync($"/api/crm/contacts/{contactId}");
        verifyResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Delete Guards

    [Fact]
    public async Task DeleteContact_WithActiveLeads_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Create a contact
        var contactRequest = CreateTestContactRequest();
        var contactResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/crm/contacts", contactRequest);
        contactResponse.EnsureSuccessStatusCode();
        var contact = await contactResponse.Content.ReadFromJsonWithEnumsAsync<ContactDto>();

        // Get or create a pipeline for the lead
        var pipelineListResponse = await adminClient.GetAsync("/api/crm/pipelines");
        pipelineListResponse.EnsureSuccessStatusCode();
        var pipelines = await pipelineListResponse.Content.ReadFromJsonWithEnumsAsync<List<PipelineDto>>();
        PipelineDto pipeline;
        if (pipelines != null && pipelines.Count > 0)
        {
            pipeline = pipelines[0];
        }
        else
        {
            var pipelineRequest = new CreatePipelineRequest(
                Name: $"Test Pipeline {Guid.NewGuid():N}",
                IsDefault: false,
                Stages: new List<CreatePipelineStageDto>
                {
                    new("Prospect", 1, "#6366f1"),
                    new("Qualified", 2, "#8b5cf6")
                });
            var createPipelineResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/crm/pipelines", pipelineRequest);
            createPipelineResponse.EnsureSuccessStatusCode();
            pipeline = (await createPipelineResponse.Content.ReadFromJsonWithEnumsAsync<PipelineDto>())!;
        }

        // Create a lead linked to the contact
        var leadRequest = new CreateLeadRequest(
            Title: $"Delete Guard Lead {Guid.NewGuid():N}",
            ContactId: contact!.Id,
            PipelineId: pipeline.Id,
            Value: 5000,
            Currency: "USD",
            Notes: "Contact delete guard test");
        var leadResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/crm/leads", leadRequest);
        leadResponse.EnsureSuccessStatusCode();

        // Act - try to delete the contact with active leads
        var response = await adminClient.DeleteAsync($"/api/crm/contacts/{contact.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Helper Methods

    private static CreateContactRequest CreateTestContactRequest()
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        return new CreateContactRequest(
            FirstName: $"Test-{uniqueId[..8]}",
            LastName: $"Contact-{uniqueId[8..16]}",
            Email: $"contact-{uniqueId}@test.com",
            Source: ContactSource.Web,
            Phone: "+1555000000",
            JobTitle: "Test Engineer");
    }

    #endregion
}
