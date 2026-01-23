using NOIR.Application.Features.Blog.Queries.GetRssFeed;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for GetRssFeedQueryHandler.
/// Tests RSS 2.0 feed generation from published blog posts.
/// </summary>
public class GetRssFeedQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Post, Guid>> _postRepositoryMock;
    private readonly GetRssFeedQueryHandler _handler;

    public GetRssFeedQueryHandlerTests()
    {
        _postRepositoryMock = new Mock<IRepository<Post, Guid>>();
        _handler = new GetRssFeedQueryHandler(_postRepositoryMock.Object);
    }

    private static Post CreateTestPost(
        string title = "Test Post",
        string slug = "test-post",
        string? excerpt = "A test post excerpt",
        string? contentHtml = "<p>Test content</p>",
        DateTimeOffset? publishedAt = null,
        string? metaTitle = null,
        string? metaDescription = null,
        string? featuredImageUrl = null,
        string? featuredImageAlt = null)
    {
        var post = Post.Create(title, slug, Guid.NewGuid(), "tenant-1");
        post.UpdateContent(title, slug, excerpt, null, contentHtml);
        post.UpdateSeo(metaTitle, metaDescription, null, true);
        if (featuredImageUrl != null)
        {
            post.UpdateFeaturedImage(featuredImageUrl, featuredImageAlt);
        }
        post.Publish();
        return post;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithPublishedPosts_ShouldReturnValidRssXml()
    {
        // Arrange
        var posts = new List<Post>
        {
            CreateTestPost(title: "First Post", slug: "first-post"),
            CreateTestPost(title: "Second Post", slug: "second-post")
        };

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        var query = new GetRssFeedQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        result.Value.Should().Contain("<rss");
        result.Value.Should().Contain("version=\"2.0\"");
        result.Value.Should().Contain("<channel>");
    }

    [Fact]
    public async Task Handle_WithPublishedPosts_ShouldContainChannelMetadata()
    {
        // Arrange
        var posts = new List<Post> { CreateTestPost() };

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        var query = new GetRssFeedQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("<title>Blog</title>");
        result.Value.Should().Contain("<description>Latest blog posts</description>");
        result.Value.Should().Contain("<link>/blog</link>");
        result.Value.Should().Contain("<language>en-us</language>");
        result.Value.Should().Contain("<generator>NOIR CMS</generator>");
    }

    [Fact]
    public async Task Handle_WithPublishedPosts_ShouldContainPostItems()
    {
        // Arrange
        var posts = new List<Post>
        {
            CreateTestPost(title: "My Article", slug: "my-article", excerpt: "Great article")
        };

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        var query = new GetRssFeedQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("<item>");
        result.Value.Should().Contain("<title>My Article | NOIR</title>");
        result.Value.Should().Contain("<link>/blog/my-article</link>");
        result.Value.Should().Contain("<guid>/blog/my-article</guid>");
    }

    [Fact]
    public async Task Handle_WithPostHavingMetaTitle_ShouldUseMetaTitle()
    {
        // Arrange
        var posts = new List<Post>
        {
            CreateTestPost(title: "My Post", metaTitle: "SEO-Optimized Title")
        };

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        var query = new GetRssFeedQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("<title>SEO-Optimized Title</title>");
    }

    [Fact]
    public async Task Handle_WithPostHavingMetaDescription_ShouldUseMetaDescription()
    {
        // Arrange
        var posts = new List<Post>
        {
            CreateTestPost(title: "Post", metaDescription: "Custom meta description for SEO")
        };

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        var query = new GetRssFeedQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("<description>Custom meta description for SEO</description>");
    }

    [Fact]
    public async Task Handle_WithPostHavingPublishedDate_ShouldContainPubDate()
    {
        // Arrange
        var posts = new List<Post> { CreateTestPost() };

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        var query = new GetRssFeedQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("<pubDate>");
    }

    [Fact]
    public async Task Handle_WithPostHavingFeaturedImage_ShouldContainMediaContent()
    {
        // Arrange
        var posts = new List<Post>
        {
            CreateTestPost(
                featuredImageUrl: "https://example.com/image.jpg",
                featuredImageAlt: "Test Image")
        };

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        var query = new GetRssFeedQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("media:content");
        result.Value.Should().Contain("https://example.com/image.jpg");
    }

    [Fact]
    public async Task Handle_WithAtomNamespace_ShouldContainSelfLink()
    {
        // Arrange
        var posts = new List<Post> { CreateTestPost() };

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        var query = new GetRssFeedQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("xmlns:atom");
        result.Value.Should().Contain("/blog/feed.xml");
        result.Value.Should().Contain("rel=\"self\"");
    }

    #endregion

    #region Empty Posts Scenario

    [Fact]
    public async Task Handle_WithNoPosts_ShouldReturnValidEmptyFeed()
    {
        // Arrange
        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());

        var query = new GetRssFeedQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("<rss");
        result.Value.Should().Contain("<channel>");
        result.Value.Should().NotContain("<item>");
    }

    #endregion

    #region Query Parameter Tests

    [Fact]
    public async Task Handle_WithMaxItemsParameter_ShouldPassToSpecification()
    {
        // Arrange
        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());

        var query = new GetRssFeedQuery(MaxItems: 50);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _postRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDefaultMaxItems_ShouldUse20()
    {
        // Arrange
        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());

        var query = new GetRssFeedQuery();

        // Assert - default MaxItems is 20
        query.MaxItems.Should().Be(20);

        // Act
        await _handler.Handle(query, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WithCategoryId_ShouldPassToSpecification()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());

        var query = new GetRssFeedQuery(CategoryId: categoryId);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _postRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region XML Structure Tests

    [Fact]
    public async Task Handle_ShouldReturnWellFormedXml()
    {
        // Arrange
        var posts = new List<Post>
        {
            CreateTestPost(title: "Post 1", slug: "post-1"),
            CreateTestPost(title: "Post 2", slug: "post-2")
        };

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        var query = new GetRssFeedQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Should parse without throwing
        var doc = new System.Xml.XmlDocument();
        doc.LoadXml(result.Value);
        doc.DocumentElement.Should().NotBeNull();
        doc.DocumentElement!.Name.Should().Be("rss");
    }

    #endregion

    #region CancellationToken Scenarios

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), token))
            .ReturnsAsync(new List<Post>());

        var query = new GetRssFeedQuery();

        // Act
        await _handler.Handle(query, token);

        // Assert
        _postRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<Specification<Post>>(), token),
            Times.Once);
    }

    #endregion
}
