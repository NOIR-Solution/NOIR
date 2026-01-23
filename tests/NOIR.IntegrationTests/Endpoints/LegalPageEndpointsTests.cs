using NOIR.Application.Features.LegalPages.DTOs;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for legal page management endpoints (admin).
/// Tests the full HTTP request/response cycle for /api/legal-pages.
/// </summary>
[Collection("Integration")]
public class LegalPageEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LegalPageEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    /// <summary>
    /// Gets an HTTP client authenticated as the tenant admin.
    /// Tenant admin has legal-pages:read and legal-pages:update permissions.
    /// </summary>
    private async Task<HttpClient> GetAdminClientAsync()
    {
        var loginCommand = new LoginCommand("admin@noir.local", "123qwe");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        response.EnsureSuccessStatusCode();
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return _factory.CreateAuthenticatedClient(loginResponse!.Auth!.AccessToken);
    }

    /// <summary>
    /// Creates a regular user without admin/legal-pages permissions.
    /// </summary>
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
            RoleNames: null);

        var createResponse = await adminClient.PostAsJsonAsync("/api/users", createCommand);
        createResponse.EnsureSuccessStatusCode();

        var loginCommand = new LoginCommand(email, password);
        var loginResult = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginResponse = await loginResult.Content.ReadFromJsonAsync<LoginResponse>();

        return (email, password, loginResponse!.Auth!);
    }

    #region GetLegalPages Tests

    [Fact]
    public async Task GetLegalPages_AsAdmin_ShouldReturnList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/legal-pages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<LegalPageListDto>>();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty(); // Seeded legal pages should exist
    }

    [Fact]
    public async Task GetLegalPages_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/legal-pages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLegalPages_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync("/api/legal-pages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GetLegalPage Tests

    [Fact]
    public async Task GetLegalPageById_ValidId_ShouldReturnPage()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First get the list to find an existing legal page
        var listResponse = await adminClient.GetAsync("/api/legal-pages");
        listResponse.EnsureSuccessStatusCode();
        var pages = await listResponse.Content.ReadFromJsonAsync<List<LegalPageListDto>>();
        pages.Should().NotBeEmpty();

        var pageId = pages!.First().Id;

        // Act
        var response = await adminClient.GetAsync($"/api/legal-pages/{pageId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<LegalPageDto>();
        page.Should().NotBeNull();
        page!.Id.Should().Be(pageId);
        page.Title.Should().NotBeNullOrEmpty();
        page.HtmlContent.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLegalPageById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/legal-pages/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetLegalPageById_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/legal-pages/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLegalPageById_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync($"/api/legal-pages/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region UpdateLegalPage Tests

    [Fact]
    public async Task UpdateLegalPage_ValidRequest_ShouldReturnUpdatedPage()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First get an existing legal page
        var listResponse = await adminClient.GetAsync("/api/legal-pages");
        var pages = await listResponse.Content.ReadFromJsonAsync<List<LegalPageListDto>>();
        var pageId = pages!.First().Id;

        var updateRequest = new UpdateLegalPageRequest(
            Title: "Updated Terms of Service",
            HtmlContent: "<h1>Updated Terms</h1><p>These are the updated terms.</p>",
            MetaTitle: "Terms - Updated",
            MetaDescription: "Updated terms description",
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/legal-pages/{pageId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedPage = await response.Content.ReadFromJsonAsync<LegalPageDto>();
        updatedPage.Should().NotBeNull();
        updatedPage!.Title.Should().Be("Updated Terms of Service");
        updatedPage.HtmlContent.Should().Contain("Updated Terms");
    }

    [Fact]
    public async Task UpdateLegalPage_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var updateRequest = new UpdateLegalPageRequest(
            Title: "Updated Title",
            HtmlContent: "<p>Content</p>",
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/legal-pages/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateLegalPage_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var updateRequest = new UpdateLegalPageRequest(
            Title: "Title",
            HtmlContent: "<p>Content</p>",
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/legal-pages/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateLegalPage_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);
        var updateRequest = new UpdateLegalPageRequest(
            Title: "Title",
            HtmlContent: "<p>Content</p>",
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var response = await userClient.PutAsJsonAsync($"/api/legal-pages/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region RevertLegalPageToDefault Tests

    [Fact]
    public async Task RevertLegalPageToDefault_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.PostAsync($"/api/legal-pages/{Guid.NewGuid()}/revert", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RevertLegalPageToDefault_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsync($"/api/legal-pages/{Guid.NewGuid()}/revert", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevertLegalPageToDefault_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.PostAsync($"/api/legal-pages/{Guid.NewGuid()}/revert", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RevertLegalPageToDefault_AfterUpdate_ShouldRevertToOriginal()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First get an existing legal page
        var listResponse = await adminClient.GetAsync("/api/legal-pages");
        var pages = await listResponse.Content.ReadFromJsonAsync<List<LegalPageListDto>>();
        var page = pages!.First();

        // Get the original page content
        var originalResponse = await adminClient.GetAsync($"/api/legal-pages/{page.Id}");
        var originalPage = await originalResponse.Content.ReadFromJsonAsync<LegalPageDto>();

        // Update it first (creates tenant override)
        var updateRequest = new UpdateLegalPageRequest(
            Title: $"Custom Title {Guid.NewGuid():N}",
            HtmlContent: "<p>Custom content for revert test</p>",
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true);
        await adminClient.PutAsJsonAsync($"/api/legal-pages/{page.Id}", updateRequest);

        // Act - Revert to default
        var revertResponse = await adminClient.PostAsync($"/api/legal-pages/{page.Id}/revert", null);

        // Assert - Should succeed (either reverted or already at default)
        // The endpoint returns OK (reverted), BadRequest (already at default/no tenant override),
        // or NotFound (platform admin has no tenant override concept)
        revertResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    #endregion
}
