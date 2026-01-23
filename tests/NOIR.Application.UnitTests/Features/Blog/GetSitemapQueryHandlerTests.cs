using NOIR.Application.Features.Blog.Queries.GetSitemap;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for GetSitemapQueryHandler.
/// Tests XML sitemap generation for blog posts and categories.
/// </summary>
public class GetSitemapQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Post, Guid>> _postRepositoryMock;
    private readonly Mock<IRepository<PostCategory, Guid>> _categoryRepositoryMock;
    private readonly GetSitemapQueryHandler _handler;

    public GetSitemapQueryHandlerTests()
    {
        _postRepositoryMock = new Mock<IRepository<Post, Guid>>();
        _categoryRepositoryMock = new Mock<IRepository<PostCategory, Guid>>();
        _handler = new GetSitemapQueryHandler(
            _postRepositoryMock.Object,
            _categoryRepositoryMock.Object);
    }

    private static Post CreateTestPost(
        string title = "Test Post",
        string slug = "test-post",
        string? featuredImageUrl = null)
    {
        var post = Post.Create(title, slug, Guid.NewGuid(), "tenant-1");
        post.UpdateContent(title, slug, "Excerpt", null, "<p>Content</p>");
        if (featuredImageUrl != null)
        {
            post.UpdateFeaturedImage(featuredImageUrl, "Alt text");
        }
        post.Publish();
        return post;
    }

    private static PostCategory CreateTestCategory(
        string name = "Test Category",
        string slug = "test-category",
        string? imageUrl = null)
    {
        var category = PostCategory.Create(name, slug, "Description", null, "tenant-1");
        if (imageUrl != null)
        {
            category.UpdateImage(imageUrl);
        }
        return category;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithPostsAndCategories_ShouldReturnValidSitemapXml()
    {
        // Arrange
        var posts = new List<Post> { CreateTestPost() };
        var categories = new List<PostCategory> { CreateTestCategory() };

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);
        _categoryRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<PostCategory>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var query = new GetSitemapQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        result.Value.Should().Contain("<urlset");
        result.Value.Should().Contain("http://www.sitemaps.org/schemas/sitemap/0.9");
    }

    [Fact]
    public async Task Handle_ShouldIncludeBlogIndexPage()
    {
        // Arrange
        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());
        _categoryRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<PostCategory>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostCategory>());

        var query = new GetSitemapQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("<loc>/blog</loc>");
        result.Value.Should().Contain("<priority>0.9</priority>");
        result.Value.Should().Contain("<changefreq>daily</changefreq>");
    }

    [Fact]
    public async Task Handle_WithPosts_ShouldIncludePostUrls()
    {
        // Arrange
        var posts = new List<Post>
        {
            CreateTestPost(title: "First Article", slug: "first-article"),
            CreateTestPost(title: "Second Article", slug: "second-article")
        };

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);
        _categoryRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<PostCategory>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostCategory>());

        var query = new GetSitemapQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("<loc>/blog/first-article</loc>");
        result.Value.Should().Contain("<loc>/blog/second-article</loc>");
        result.Value.Should().Contain("<priority>0.8</priority>");
        result.Value.Should().Contain("<changefreq>weekly</changefreq>");
    }

    [Fact]
    public async Task Handle_WithCategories_ShouldIncludeCategoryUrls()
    {
        // Arrange
        var categories = new List<PostCategory>
        {
            CreateTestCategory(name: "Tech", slug: "tech"),
            CreateTestCategory(name: "Science", slug: "science")
        };

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());
        _categoryRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<PostCategory>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var query = new GetSitemapQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("<loc>/blog/category/tech</loc>");
        result.Value.Should().Contain("<loc>/blog/category/science</loc>");
        result.Value.Should().Contain("<priority>0.6</priority>");
    }

    #endregion

    #region Image Inclusion Tests

    [Fact]
    public async Task Handle_WithIncludeImagesTrue_ShouldIncludeImageNamespace()
    {
        // Arrange
        var posts = new List<Post> { CreateTestPost(featuredImageUrl: "https://example.com/image.jpg") };

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);
        _categoryRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<PostCategory>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostCategory>());

        var query = new GetSitemapQuery(IncludeImages: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("http://www.google.com/schemas/sitemap-image/1.1");
        result.Value.Should().Contain("https://example.com/image.jpg");
    }

    [Fact]
    public async Task Handle_WithIncludeImagesFalse_ShouldNotIncludeImageNamespace()
    {
        // Arrange
        var posts = new List<Post> { CreateTestPost(featuredImageUrl: "https://example.com/image.jpg") };

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);
        _categoryRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<PostCategory>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostCategory>());

        var query = new GetSitemapQuery(IncludeImages: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotContain("xmlns:image");
    }

    [Fact]
    public async Task Handle_WithCategoryHavingImage_ShouldIncludeImageData()
    {
        // Arrange
        var categories = new List<PostCategory>
        {
            CreateTestCategory(name: "Design", slug: "design", imageUrl: "https://example.com/design.jpg")
        };

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());
        _categoryRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<PostCategory>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var query = new GetSitemapQuery(IncludeImages: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("https://example.com/design.jpg");
    }

    #endregion

    #region Empty Content Scenarios

    [Fact]
    public async Task Handle_WithNoPostsOrCategories_ShouldReturnSitemapWithOnlyBlogIndex()
    {
        // Arrange
        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());
        _categoryRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<PostCategory>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostCategory>());

        var query = new GetSitemapQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("<urlset");
        result.Value.Should().Contain("<loc>/blog</loc>");
    }

    #endregion

    #region XML Structure Tests

    [Fact]
    public async Task Handle_ShouldReturnWellFormedXml()
    {
        // Arrange
        var posts = new List<Post> { CreateTestPost() };
        var categories = new List<PostCategory> { CreateTestCategory() };

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);
        _categoryRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<PostCategory>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var query = new GetSitemapQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var doc = new System.Xml.XmlDocument();
        doc.LoadXml(result.Value);
        doc.DocumentElement.Should().NotBeNull();
        doc.DocumentElement!.Name.Should().Be("urlset");
    }

    [Fact]
    public async Task Handle_PostUrlsShouldContainLastmod()
    {
        // Arrange
        var posts = new List<Post> { CreateTestPost() };

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<Post>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);
        _categoryRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<PostCategory>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostCategory>());

        var query = new GetSitemapQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("<lastmod>");
    }

    #endregion

    #region Query Default Values

    [Fact]
    public void GetSitemapQuery_DefaultIncludeImages_ShouldBeTrue()
    {
        // Arrange & Act
        var query = new GetSitemapQuery();

        // Assert
        query.IncludeImages.Should().BeTrue();
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
        _categoryRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<Specification<PostCategory>>(), token))
            .ReturnsAsync(new List<PostCategory>());

        var query = new GetSitemapQuery();

        // Act
        await _handler.Handle(query, token);

        // Assert
        _postRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<Specification<Post>>(), token),
            Times.Once);
        _categoryRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<Specification<PostCategory>>(), token),
            Times.Once);
    }

    #endregion
}
