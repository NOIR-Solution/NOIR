using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Queries.GetPost;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for GetPostQueryHandler.
/// Tests single post retrieval by ID or slug.
/// </summary>
public class GetPostQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Post, Guid>> _repositoryMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly GetPostQueryHandler _handler;

    public GetPostQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<Post, Guid>>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.TenantId).Returns("test-tenant");
        _handler = new GetPostQueryHandler(_repositoryMock.Object, _currentUserMock.Object);
    }

    private static Post CreateTestPost(
        Guid? id = null,
        string title = "Test Post",
        string slug = "test-post",
        Guid? authorId = null,
        PostStatus status = PostStatus.Draft)
    {
        var post = Post.Create(
            title,
            slug,
            authorId ?? Guid.NewGuid(),
            tenantId: "test-tenant");

        // Use reflection to set the ID if provided
        if (id.HasValue)
        {
            var idProperty = typeof(Post).GetProperty("Id");
            idProperty?.SetValue(post, id.Value);
        }

        if (status == PostStatus.Published)
        {
            post.Publish();
        }

        return post;
    }

    #endregion

    #region Success Scenarios - By ID

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnPost()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(id: postId, title: "Found Post", slug: "found-post");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var query = new GetPostQuery(Id: postId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Title.Should().Be("Found Post");
        result.Value.Slug.Should().Be("found-post");
    }

    [Fact]
    public async Task Handle_WithValidId_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var post = CreateTestPost(
            id: postId,
            title: "Complete Post",
            slug: "complete-post",
            authorId: authorId,
            status: PostStatus.Published);

        post.UpdateContent(
            "Complete Post",
            "complete-post",
            "This is the excerpt",
            "[{\"type\":\"paragraph\"}]",
            "<p>This is HTML content</p>");

        post.UpdateSeo(
            "SEO Title",
            "SEO Description",
            "https://example.com/canonical",
            allowIndexing: true);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var query = new GetPostQuery(Id: postId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Title.Should().Be("Complete Post");
        dto.Slug.Should().Be("complete-post");
        dto.Excerpt.Should().Be("This is the excerpt");
        dto.ContentJson.Should().Be("[{\"type\":\"paragraph\"}]");
        dto.ContentHtml.Should().Be("<p>This is HTML content</p>");
        dto.Status.Should().Be(PostStatus.Published);
        dto.MetaTitle.Should().Be("SEO Title");
        dto.MetaDescription.Should().Be("SEO Description");
        dto.CanonicalUrl.Should().Be("https://example.com/canonical");
        dto.AllowIndexing.Should().BeTrue();
        dto.AuthorId.Should().Be(authorId);
    }

    #endregion

    #region Success Scenarios - By Slug

    [Fact]
    public async Task Handle_WithValidSlug_ShouldReturnPost()
    {
        // Arrange
        var post = CreateTestPost(title: "Slug Post", slug: "slug-post");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var query = new GetPostQuery(Slug: "slug-post");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Slug.Should().Be("slug-post");
    }

    [Fact]
    public async Task Handle_WithSlug_ShouldUseTenantIdFromCurrentUser()
    {
        // Arrange
        var post = CreateTestPost(title: "Tenant Post", slug: "tenant-post");

        _currentUserMock.Setup(x => x.TenantId).Returns("specific-tenant");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var query = new GetPostQuery(Slug: "tenant-post");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _currentUserMock.Verify(x => x.TenantId, Times.Once);
    }

    #endregion

    #region Not Found Scenarios

    [Fact]
    public async Task Handle_WithInvalidId_ShouldReturnNotFoundError()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        var query = new GetPostQuery(Id: invalidId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.Code.Should().Be("NOIR-BLOG-003");
    }

    [Fact]
    public async Task Handle_WithInvalidSlug_ShouldReturnNotFoundError()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        var query = new GetPostQuery(Slug: "non-existent-slug");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.Code.Should().Be("NOIR-BLOG-003");
    }

    #endregion

    #region Validation Scenarios

    [Fact]
    public async Task Handle_WithNeitherIdNorSlug_ShouldReturnValidationError()
    {
        // Arrange
        var query = new GetPostQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.Code.Should().Be("NOIR-BLOG-013");
    }

    [Fact]
    public async Task Handle_WithEmptySlug_ShouldReturnValidationError()
    {
        // Arrange
        var query = new GetPostQuery(Slug: "   ");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.Code.Should().Be("NOIR-BLOG-013");
    }

    [Fact]
    public async Task Handle_WithBothIdAndSlug_ShouldPrioritizeId()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(id: postId, title: "ID Priority Post", slug: "id-priority-post");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var query = new GetPostQuery(Id: postId, Slug: "different-slug");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("ID Priority Post");
    }

    #endregion

    #region Post With Tags

    [Fact]
    public async Task Handle_WithTags_ShouldMapTagsCorrectly()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(id: postId, title: "Tagged Post", slug: "tagged-post");

        // Note: In a real scenario, the tags would be loaded via the specification's Include.
        // Since we're mocking, we verify the handler doesn't fail when TagAssignments is empty.

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var query = new GetPostQuery(Id: postId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tags.Should().NotBeNull();
        result.Value.Tags.Should().BeEmpty(); // Empty because TagAssignments is empty in mock
    }

    #endregion

    #region Post Status Scenarios

    [Fact]
    public async Task Handle_WithDraftPost_ShouldReturnPost()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(id: postId, title: "Draft Post", slug: "draft-post", status: PostStatus.Draft);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var query = new GetPostQuery(Id: postId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PostStatus.Draft);
    }

    [Fact]
    public async Task Handle_WithPublishedPost_ShouldIncludePublishedAt()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(id: postId, title: "Published Post", slug: "published-post", status: PostStatus.Published);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var query = new GetPostQuery(Id: postId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PostStatus.Published);
        result.Value.PublishedAt.Should().NotBeNull();
    }

    #endregion
}
