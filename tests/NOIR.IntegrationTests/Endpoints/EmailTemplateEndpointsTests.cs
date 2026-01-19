using NOIR.Application.Features.EmailTemplates.DTOs;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for email template endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class EmailTemplateEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EmailTemplateEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    private async Task<HttpClient> GetAdminClientAsync()
    {
        var loginCommand = new LoginCommand("admin@noir.local", "123qwe");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return _factory.CreateAuthenticatedClient(auth!.AccessToken);
    }

    private async Task<(string Email, string Password, AuthResponse Auth)> CreateTestUserAsync()
    {
        var adminClient = await GetAdminClientAsync();
        var email = $"test_{Guid.NewGuid():N}@example.com";
        var password = "TestPassword123!";

        var createCommand = new CreateUserCommand(
            Email: email,
            Password: password,
            FirstName: "Test",
            LastName: "User",
            DisplayName: null,
            RoleNames: null); // No roles - regular user without admin permissions

        var createResponse = await adminClient.PostAsJsonAsync("/api/users", createCommand);
        createResponse.EnsureSuccessStatusCode();

        // Login as the created user
        var loginCommand = new LoginCommand(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        return (email, password, auth!);
    }

    #region Get Email Templates Tests

    [Fact]
    public async Task GetEmailTemplates_AsAdmin_ShouldReturnList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/email-templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<EmailTemplateListDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetEmailTemplates_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/email-templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetEmailTemplates_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without email template permissions
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync("/api/email-templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetEmailTemplates_WithSearch_ShouldFilterResults()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/email-templates?search=welcome");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<EmailTemplateListDto>>();
        result.Should().NotBeNull();
    }

    #endregion

    #region Get Email Template By Id Tests

    [Fact]
    public async Task GetEmailTemplateById_ValidId_ShouldReturnTemplate()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First get the list to find a template ID
        var listResponse = await adminClient.GetAsync("/api/email-templates");
        var templates = await listResponse.Content.ReadFromJsonAsync<List<EmailTemplateListDto>>();

        // Skip if no templates exist
        if (templates is null || templates.Count == 0)
        {
            return;
        }

        var templateId = templates.First().Id;

        // Act
        var response = await adminClient.GetAsync($"/api/email-templates/{templateId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var template = await response.Content.ReadFromJsonAsync<EmailTemplateDto>();
        template.Should().NotBeNull();
        template!.Id.Should().Be(templateId);
    }

    [Fact]
    public async Task GetEmailTemplateById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/email-templates/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEmailTemplateById_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/email-templates/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Update Email Template Tests

    [Fact]
    public async Task UpdateEmailTemplate_ValidRequest_ShouldReturnUpdatedTemplate()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First get the list to find a template ID
        var listResponse = await adminClient.GetAsync("/api/email-templates");
        var templates = await listResponse.Content.ReadFromJsonAsync<List<EmailTemplateListDto>>();

        // Skip if no templates exist
        if (templates is null || templates.Count == 0)
        {
            return;
        }

        var templateId = templates.First().Id;
        var updateRequest = new UpdateEmailTemplateRequest(
            Subject: "Updated Subject {{UserName}}",
            HtmlBody: "<h1>Updated Content</h1>",
            PlainTextBody: "Updated plain text",
            Description: "Updated description");

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/email-templates/{templateId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedTemplate = await response.Content.ReadFromJsonAsync<EmailTemplateDto>();
        updatedTemplate.Should().NotBeNull();
        updatedTemplate!.Subject.Should().Be("Updated Subject {{UserName}}");
    }

    [Fact]
    public async Task UpdateEmailTemplate_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var updateRequest = new UpdateEmailTemplateRequest(
            Subject: "Updated Subject",
            HtmlBody: "<h1>Updated</h1>",
            PlainTextBody: null,
            Description: null);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/email-templates/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateEmailTemplate_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without email template update permissions
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);
        var updateRequest = new UpdateEmailTemplateRequest(
            Subject: "Updated Subject",
            HtmlBody: "<h1>Updated</h1>",
            PlainTextBody: null,
            Description: null);

        // Act
        var response = await userClient.PutAsJsonAsync($"/api/email-templates/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateEmailTemplate_EmptySubject_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First get the list to find a template ID
        var listResponse = await adminClient.GetAsync("/api/email-templates");
        var templates = await listResponse.Content.ReadFromJsonAsync<List<EmailTemplateListDto>>();

        // Skip if no templates exist
        if (templates is null || templates.Count == 0)
        {
            return;
        }

        var templateId = templates.First().Id;
        var updateRequest = new UpdateEmailTemplateRequest(
            Subject: "",
            HtmlBody: "<h1>Content</h1>",
            PlainTextBody: null,
            Description: null);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/email-templates/{templateId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Preview Email Template Tests

    [Fact]
    public async Task PreviewEmailTemplate_ValidId_ShouldReturnPreview()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First get the list to find a template ID
        var listResponse = await adminClient.GetAsync("/api/email-templates");
        var templates = await listResponse.Content.ReadFromJsonAsync<List<EmailTemplateListDto>>();

        // Skip if no templates exist
        if (templates is null || templates.Count == 0)
        {
            return;
        }

        var templateId = templates.First().Id;
        var previewRequest = new PreviewEmailTemplateRequest(
            SampleData: new Dictionary<string, string>
            {
                ["UserName"] = "John Doe",
                ["Email"] = "john@example.com"
            });

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/email-templates/{templateId}/preview", previewRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = await response.Content.ReadFromJsonAsync<EmailPreviewResponse>();
        preview.Should().NotBeNull();
    }

    [Fact]
    public async Task PreviewEmailTemplate_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var previewRequest = new PreviewEmailTemplateRequest(
            SampleData: new Dictionary<string, string>());

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/email-templates/{Guid.NewGuid()}/preview", previewRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Send Test Email Tests

    [Fact]
    public async Task SendTestEmail_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var testRequest = new SendTestEmailRequest(
            RecipientEmail: "test@example.com",
            SampleData: new Dictionary<string, string>
            {
                ["UserName"] = "Test User"
            });

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/email-templates/{Guid.NewGuid()}/test", testRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SendTestEmail_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without email template update permissions
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);
        var testRequest = new SendTestEmailRequest(
            RecipientEmail: "test@example.com",
            SampleData: new Dictionary<string, string>());

        // Act
        var response = await userClient.PostAsJsonAsync($"/api/email-templates/{Guid.NewGuid()}/test", testRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SendTestEmail_InvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First get the list to find a template ID
        var listResponse = await adminClient.GetAsync("/api/email-templates");
        var templates = await listResponse.Content.ReadFromJsonAsync<List<EmailTemplateListDto>>();

        // Skip if no templates exist
        if (templates is null || templates.Count == 0)
        {
            return;
        }

        var templateId = templates.First().Id;
        var testRequest = new SendTestEmailRequest(
            RecipientEmail: "not-an-email",
            SampleData: new Dictionary<string, string>());

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/email-templates/{templateId}/test", testRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
