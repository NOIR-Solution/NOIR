using NOIR.Application.Features.Media.Dtos;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for media upload and management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class MediaEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MediaEndpointsTests(CustomWebApplicationFactory factory)
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

        // Login as the created user
        var loginCommand = new LoginCommand(email, password);
        var loginResult = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginResponse = await loginResult.Content.ReadFromJsonAsync<LoginResponse>();

        return (email, password, loginResponse!.Auth!);
    }

    private static MultipartFormDataContent CreateTestImageContent(string filename = "test.jpg")
    {
        // Create a minimal valid JPEG image (smallest possible valid JPEG)
        var minimalJpegBytes = new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43,
            0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08, 0x07, 0x07, 0x07, 0x09,
            0x09, 0x08, 0x0A, 0x0C, 0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12,
            0x13, 0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A, 0x1C, 0x1C, 0x20,
            0x24, 0x2E, 0x27, 0x20, 0x22, 0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29,
            0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27, 0x39, 0x3D, 0x38, 0x32,
            0x3C, 0x2E, 0x33, 0x34, 0x32, 0xFF, 0xC0, 0x00, 0x0B, 0x08, 0x00, 0x01,
            0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0xFF, 0xC4, 0x00, 0x1F, 0x00, 0x00,
            0x01, 0x05, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0A, 0x0B, 0xFF, 0xC4, 0x00, 0xB5, 0x10, 0x00, 0x02, 0x01, 0x03,
            0x03, 0x02, 0x04, 0x03, 0x05, 0x05, 0x04, 0x04, 0x00, 0x00, 0x01, 0x7D,
            0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12, 0x21, 0x31, 0x41, 0x06,
            0x13, 0x51, 0x61, 0x07, 0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xA1, 0x08,
            0x23, 0x42, 0xB1, 0xC1, 0x15, 0x52, 0xD1, 0xF0, 0x24, 0x33, 0x62, 0x72,
            0x82, 0x09, 0x0A, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x25, 0x26, 0x27, 0x28,
            0x29, 0x2A, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x43, 0x44, 0x45,
            0x46, 0x47, 0x48, 0x49, 0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
            0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x73, 0x74, 0x75,
            0x76, 0x77, 0x78, 0x79, 0x7A, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
            0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3,
            0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6,
            0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9,
            0xCA, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xE1, 0xE2,
            0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xF1, 0xF2, 0xF3, 0xF4,
            0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01,
            0x00, 0x00, 0x3F, 0x00, 0xFB, 0xD5, 0xDB, 0x20, 0xA8, 0xA0, 0x02, 0x80,
            0x0A, 0x00, 0xFF, 0xD9
        };

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(minimalJpegBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", filename);
        return content;
    }

    #region Upload Media Tests

    [Fact]
    public async Task UploadMedia_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var content = CreateTestImageContent();

        // Act
        var response = await _client.PostAsync("/api/media/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadMedia_NoFile_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var content = new MultipartFormDataContent();

        // Act
        var response = await adminClient.PostAsync("/api/media/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadMedia_InvalidFolder_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var content = CreateTestImageContent();

        // Act
        var response = await adminClient.PostAsync("/api/media/upload?folder=invalid-folder", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadMedia_ValidFolder_ShouldAcceptBlogFolder()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var content = CreateTestImageContent();

        // Act - Just verify it doesn't return BadRequest for valid folder
        // Note: Actual success depends on image processing which may not work with minimal JPEG
        var response = await adminClient.PostAsync("/api/media/upload?folder=blog", content);

        // Assert - Either OK (processing worked) or BadRequest for image validation (not folder validation)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);

        // If BadRequest, it should be about image processing, not folder
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            errorContent.Should().NotContain("Invalid folder");
        }
    }

    #endregion

    #region Get MediaFile By Id Tests

    [Fact]
    public async Task GetMediaFileById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/media/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMediaFileById_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/media/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get MediaFile By Slug Tests

    [Fact]
    public async Task GetMediaFileBySlug_InvalidSlug_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/media/by-slug/non-existent-slug-12345");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMediaFileBySlug_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/media/by-slug/any-slug");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get MediaFile By URL Tests

    [Fact]
    public async Task GetMediaFileByUrl_InvalidUrl_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var testUrl = Uri.EscapeDataString("/media/blog/non-existent-image.webp");

        // Act
        var response = await adminClient.GetAsync($"/api/media/by-url?url={testUrl}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMediaFileByUrl_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/media/by-url?url=test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get MediaFile By ShortId Tests

    [Fact]
    public async Task GetMediaFileByShortId_InvalidShortId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/media/by-short-id/a1b2c3d4");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMediaFileByShortId_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/media/by-short-id/any-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Batch Query By Ids Tests

    [Fact]
    public async Task GetMediaFilesByIds_EmptyList_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new { Ids = Array.Empty<Guid>() };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/media/batch/by-ids", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMediaFilesByIds_ValidRequest_ShouldReturnList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new { Ids = new[] { Guid.NewGuid() } };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/media/batch/by-ids", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<MediaFileDto>>();
        result.Should().NotBeNull();
        result.Should().BeEmpty(); // No matches for random GUIDs
    }

    [Fact]
    public async Task GetMediaFilesByIds_TooManyIds_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var ids = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var request = new { Ids = ids };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/media/batch/by-ids", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMediaFilesByIds_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new { Ids = new[] { Guid.NewGuid() } };

        // Act
        var response = await _client.PostAsJsonAsync("/api/media/batch/by-ids", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Batch Query By Slugs Tests

    [Fact]
    public async Task GetMediaFilesBySlugs_EmptyList_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new { Slugs = Array.Empty<string>() };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/media/batch/by-slugs", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMediaFilesBySlugs_ValidRequest_ShouldReturnList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new { Slugs = new[] { "test-slug" } };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/media/batch/by-slugs", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<MediaFileDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMediaFilesBySlugs_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new { Slugs = new[] { "test-slug" } };

        // Act
        var response = await _client.PostAsJsonAsync("/api/media/batch/by-slugs", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Batch Query By ShortIds Tests

    [Fact]
    public async Task GetMediaFilesByShortIds_EmptyList_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new { ShortIds = Array.Empty<string>() };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/media/batch/by-short-ids", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMediaFilesByShortIds_ValidRequest_ShouldReturnList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new { ShortIds = new[] { "a1b2c3d4" } };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/media/batch/by-short-ids", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<MediaFileDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMediaFilesByShortIds_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new { ShortIds = new[] { "a1b2c3d4" } };

        // Act
        var response = await _client.PostAsJsonAsync("/api/media/batch/by-short-ids", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region User Authorization Tests

    [Fact]
    public async Task MediaEndpoints_AuthenticatedUser_ShouldHaveAccess()
    {
        // Arrange - Create a regular user (any authenticated user can access media)
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act - Regular users should be able to access media endpoints
        var response = await userClient.GetAsync($"/api/media/{Guid.NewGuid()}");

        // Assert - Should get 404 (not found), not 403 (forbidden)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
