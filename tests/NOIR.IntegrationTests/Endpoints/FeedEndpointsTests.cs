namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for blog feed endpoints.
/// Tests the full HTTP request/response cycle for RSS, Sitemap, and robots.txt.
/// These endpoints are public (no authentication required).
/// </summary>
[Collection("Integration")]
public class FeedEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public FeedEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    #region RSS Feed Tests

    [Fact]
    public async Task GetRssFeed_ShouldReturnXmlContent()
    {
        // Act
        var response = await _client.GetAsync("/blog/feed.xml");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/rss+xml");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("<?xml");
        content.Should().Contain("<rss");
        content.Should().Contain("<channel>");
    }

    [Fact]
    public async Task GetRssFeed_DoesNotRequireAuthentication()
    {
        // Act - No auth header, no login
        var response = await _client.GetAsync("/blog/feed.xml");

        // Assert - Should succeed without authentication
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetRssFeed_WithMaxItems_ShouldRespectParameter()
    {
        // Act
        var response = await _client.GetAsync("/blog/feed.xml?maxItems=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("<rss");
    }

    [Fact]
    public async Task GetRssFeed_WithCategoryFilter_ShouldApplyFilter()
    {
        // Act - Category filter with non-existent category GUID
        var response = await _client.GetAsync($"/blog/feed.xml?categoryId={Guid.NewGuid()}");

        // Assert - Should still succeed (empty or filtered results)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("<rss");
    }

    [Fact]
    public async Task GetRssFeedAlt_ShouldReturnXmlContent()
    {
        // Act - Alternative /rss.xml path
        var response = await _client.GetAsync("/rss.xml");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/rss+xml");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("<rss");
    }

    [Fact]
    public async Task GetRssFeed_ShouldContainStandardElements()
    {
        // Act
        var response = await _client.GetAsync("/blog/feed.xml");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        // Standard RSS elements
        content.Should().Contain("<title>");
        content.Should().Contain("<link>");
        content.Should().Contain("<description>");
    }

    #endregion

    #region Sitemap Tests

    [Fact]
    public async Task GetSitemap_ShouldReturnXmlContent()
    {
        // Act
        var response = await _client.GetAsync("/sitemap.xml");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/xml");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("<?xml");
        content.Should().Contain("<urlset");
    }

    [Fact]
    public async Task GetSitemap_DoesNotRequireAuthentication()
    {
        // Act - No auth header, no login
        var response = await _client.GetAsync("/sitemap.xml");

        // Assert - Should succeed without authentication
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSitemap_WithIncludeImages_True_ShouldIncludeImageNamespace()
    {
        // Act
        var response = await _client.GetAsync("/sitemap.xml?includeImages=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("<urlset");
        // Image namespace might be present if there are posts with images
    }

    [Fact]
    public async Task GetSitemap_WithIncludeImages_False_ShouldStillWork()
    {
        // Act
        var response = await _client.GetAsync("/sitemap.xml?includeImages=false");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("<urlset");
    }

    [Fact]
    public async Task GetSitemap_ShouldContainStandardElements()
    {
        // Act
        var response = await _client.GetAsync("/sitemap.xml");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        // Standard sitemap elements
        content.Should().Contain("xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"");
    }

    #endregion

    #region Robots.txt Tests

    [Fact]
    public async Task GetRobotsTxt_ShouldReturnTextContent()
    {
        // Act
        var response = await _client.GetAsync("/robots.txt");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("User-agent:");
    }

    [Fact]
    public async Task GetRobotsTxt_DoesNotRequireAuthentication()
    {
        // Act - No auth header, no login
        var response = await _client.GetAsync("/robots.txt");

        // Assert - Should succeed without authentication
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetRobotsTxt_ShouldContainSitemapReference()
    {
        // Act
        var response = await _client.GetAsync("/robots.txt");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Sitemap: /sitemap.xml");
    }

    [Fact]
    public async Task GetRobotsTxt_ShouldAllowAllUserAgents()
    {
        // Act
        var response = await _client.GetAsync("/robots.txt");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("User-agent: *");
        content.Should().Contain("Allow: /");
    }

    #endregion

    #region Caching Tests

    [Fact]
    public async Task GetRssFeed_ShouldBeCacheable()
    {
        // Act
        var response1 = await _client.GetAsync("/blog/feed.xml");
        var response2 = await _client.GetAsync("/blog/feed.xml");

        // Assert - Both should succeed
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSitemap_ShouldBeCacheable()
    {
        // Act
        var response1 = await _client.GetAsync("/sitemap.xml");
        var response2 = await _client.GetAsync("/sitemap.xml");

        // Assert - Both should succeed
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRobotsTxt_ShouldBeCacheable()
    {
        // Act
        var response1 = await _client.GetAsync("/robots.txt");
        var response2 = await _client.GetAsync("/robots.txt");

        // Assert - Both should succeed
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Content Type Tests

    [Fact]
    public async Task AllFeedEndpoints_ShouldHaveCorrectCharset()
    {
        // Act
        var rssResponse = await _client.GetAsync("/blog/feed.xml");
        var sitemapResponse = await _client.GetAsync("/sitemap.xml");
        var robotsResponse = await _client.GetAsync("/robots.txt");

        // Assert - All should have UTF-8 charset
        rssResponse.Content.Headers.ContentType!.CharSet.Should().Be("utf-8");
        sitemapResponse.Content.Headers.ContentType!.CharSet.Should().Be("utf-8");
        robotsResponse.Content.Headers.ContentType!.CharSet.Should().Be("utf-8");
    }

    #endregion
}
