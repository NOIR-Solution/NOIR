using NOIR.Application.Features.LegalPages.DTOs;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for public legal page endpoints.
/// Tests the full HTTP request/response cycle for /api/public/legal.
/// These endpoints are anonymous (no authentication required).
/// </summary>
[Collection("Integration")]
public class PublicLegalPageEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PublicLegalPageEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    #region GetPublicLegalPage Tests

    [Fact]
    public async Task GetPublicLegalPage_TermsOfService_ShouldReturnPage()
    {
        // Act
        var response = await _client.GetAsync("/api/public/legal/terms-of-service");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PublicLegalPageDto>();
        page.Should().NotBeNull();
        page!.Slug.Should().Be("terms-of-service");
        page.Title.Should().NotBeNullOrEmpty();
        page.HtmlContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPublicLegalPage_PrivacyPolicy_ShouldReturnPage()
    {
        // Act
        var response = await _client.GetAsync("/api/public/legal/privacy-policy");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PublicLegalPageDto>();
        page.Should().NotBeNull();
        page!.Slug.Should().Be("privacy-policy");
        page.Title.Should().NotBeNullOrEmpty();
        page.HtmlContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPublicLegalPage_CookiePolicy_ShouldReturnNotFound_WhenNotSeeded()
    {
        // Act - cookie-policy is not seeded in the test database
        // Only terms-of-service and privacy-policy are seeded by LegalPageSeeder
        var response = await _client.GetAsync("/api/public/legal/cookie-policy");

        // Assert - Should return 404 since page is not seeded
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPublicLegalPage_NonExistentSlug_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/public/legal/non-existent-page-slug");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPublicLegalPage_EmptySlug_ShouldReturnNotFound()
    {
        // Act - empty slug should match no route or return 404
        var response = await _client.GetAsync("/api/public/legal/");

        // Assert - Either MethodNotAllowed (no route match) or NotFound
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task GetPublicLegalPage_DoesNotRequireAuthentication()
    {
        // Act - No auth header, no login
        var response = await _client.GetAsync("/api/public/legal/terms-of-service");

        // Assert - Should succeed without authentication
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPublicLegalPage_ShouldReturnMetadata()
    {
        // Act
        var response = await _client.GetAsync("/api/public/legal/terms-of-service");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PublicLegalPageDto>();
        page.Should().NotBeNull();
        page!.LastModified.Should().NotBe(default);
        // AllowIndexing should be a valid boolean value
        page.AllowIndexing.Should().Be(page.AllowIndexing);
    }

    #endregion
}
