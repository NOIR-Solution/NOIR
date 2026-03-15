using NOIR.Application.Features.Blog.Commands.CreatePost;
using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Services;
using NOIR.Application.Features.Blog.Specifications;
using NOIR.Domain.ValueObjects;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for CreatePostCommandHandler.
/// Tests post creation scenarios with mocked dependencies.
/// </summary>
public class CreatePostCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Post, Guid>> _postRepositoryMock;
    private readonly Mock<IRepository<PostTag, Guid>> _tagRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IContentAnalyzer> _contentAnalyzerMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly CreatePostCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "550e8400-e29b-41d4-a716-446655440000";

    public CreatePostCommandHandlerTests()
    {
        _postRepositoryMock = new Mock<IRepository<Post, Guid>>();
        _tagRepositoryMock = new Mock<IRepository<PostTag, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _contentAnalyzerMock = new Mock<IContentAnalyzer>();

        // Setup default current user
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        // Setup default content analyzer (returns empty metadata)
        _contentAnalyzerMock
            .Setup(x => x.Analyze(It.IsAny<string?>()))
            .Returns(new ContentMetadata());

        _handler = new CreatePostCommandHandler(
            _postRepositoryMock.Object,
            _tagRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _contentAnalyzerMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static CreatePostCommand CreateTestCommand(
        string title = "Test Post Title",
        string slug = "test-post-title",
        string? excerpt = "Test excerpt",
        string? contentJson = "{\"blocks\":[]}",
        string? contentHtml = "<p>Test content</p>",
        Guid? featuredImageId = null,
        string? featuredImageUrl = null,
        string? featuredImageAlt = null,
        string? metaTitle = null,
        string? metaDescription = null,
        string? canonicalUrl = null,
        bool allowIndexing = true,
        Guid? categoryId = null,
        List<Guid>? tagIds = null,
        string? userId = TestUserId)
    {
        return new CreatePostCommand(
            title,
            slug,
            excerpt,
            contentJson,
            contentHtml,
            featuredImageId,
            featuredImageUrl,
            featuredImageAlt,
            metaTitle,
            metaDescription,
            canonicalUrl,
            allowIndexing,
            categoryId,
            tagIds)
        { UserId = userId };
    }

    private static PostTag CreateTestTag(Guid? id = null, string name = "Test Tag", string slug = "test-tag")
    {
        var tag = PostTag.Create(name, slug, "Test description", "#3B82F6", TestTenantId);
        if (id.HasValue)
        {
            // Use reflection to set the Id for testing
            typeof(PostTag).GetProperty("Id")?.SetValue(tag, id.Value);
        }
        return tag;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSucceed()
    {
        // Arrange
        var command = CreateTestCommand();

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        _postRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.Title.ShouldBe(command.Title);
        result.Value.Slug.ShouldBe(command.Slug.ToLowerInvariant());
        result.Value.Status.ShouldBe(PostStatus.Draft);

        _postRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreatePostWithCorrectContent()
    {
        // Arrange
        var command = CreateTestCommand(
            title: "My Blog Post",
            slug: "my-blog-post",
            excerpt: "This is my excerpt",
            contentJson: "{\"type\":\"doc\",\"content\":[]}",
            contentHtml: "<p>Hello World</p>");

        Post? capturedPost = null;

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        _postRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()))
            .Callback<Post, CancellationToken>((post, _) => capturedPost = post)
            .ReturnsAsync((Post post, CancellationToken _) => post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        capturedPost.ShouldNotBeNull();
        capturedPost!.Title.ShouldBe("My Blog Post");
        capturedPost.Slug.ShouldBe("my-blog-post");
        capturedPost.Excerpt.ShouldBe("This is my excerpt");
        capturedPost.ContentJson.ShouldBe("{\"type\":\"doc\",\"content\":[]}");
        capturedPost.ContentHtml.ShouldBe("<p>Hello World</p>");
    }

    [Fact]
    public async Task Handle_WithFeaturedImageId_ShouldSetFeaturedImageId()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var command = CreateTestCommand(
            featuredImageId: imageId,
            featuredImageAlt: "Alt text for image");

        Post? capturedPost = null;

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        _postRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()))
            .Callback<Post, CancellationToken>((post, _) => capturedPost = post)
            .ReturnsAsync((Post post, CancellationToken _) => post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        capturedPost.ShouldNotBeNull();
        capturedPost!.FeaturedImageId.ShouldBe(imageId);
        capturedPost.FeaturedImageAlt.ShouldBe("Alt text for image");
    }

    [Fact]
    public async Task Handle_WithFeaturedImageUrl_ShouldSetFeaturedImageUrl()
    {
        // Arrange
        var command = CreateTestCommand(
            featuredImageUrl: "https://example.com/image.jpg",
            featuredImageAlt: "Alt text");

        Post? capturedPost = null;

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        _postRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()))
            .Callback<Post, CancellationToken>((post, _) => capturedPost = post)
            .ReturnsAsync((Post post, CancellationToken _) => post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        capturedPost.ShouldNotBeNull();
        capturedPost!.FeaturedImageUrl.ShouldBe("https://example.com/image.jpg");
        capturedPost.FeaturedImageAlt.ShouldBe("Alt text");
    }

    [Fact]
    public async Task Handle_WithSeoMetadata_ShouldSetSeoFields()
    {
        // Arrange
        var command = CreateTestCommand(
            metaTitle: "SEO Title",
            metaDescription: "SEO Description",
            canonicalUrl: "https://example.com/canonical",
            allowIndexing: false);

        Post? capturedPost = null;

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        _postRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()))
            .Callback<Post, CancellationToken>((post, _) => capturedPost = post)
            .ReturnsAsync((Post post, CancellationToken _) => post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        capturedPost.ShouldNotBeNull();
        capturedPost!.MetaTitle.ShouldBe("SEO Title");
        capturedPost.MetaDescription.ShouldBe("SEO Description");
        capturedPost.CanonicalUrl.ShouldBe("https://example.com/canonical");
        capturedPost.AllowIndexing.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_WithCategoryId_ShouldSetCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var command = CreateTestCommand(categoryId: categoryId);

        Post? capturedPost = null;

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        _postRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()))
            .Callback<Post, CancellationToken>((post, _) => capturedPost = post)
            .ReturnsAsync((Post post, CancellationToken _) => post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        capturedPost.ShouldNotBeNull();
        capturedPost!.CategoryId.ShouldBe(categoryId);
    }

    [Fact]
    public async Task Handle_WithTagIds_ShouldAssignTags()
    {
        // Arrange
        var tag1Id = Guid.NewGuid();
        var tag2Id = Guid.NewGuid();
        var tagIds = new List<Guid> { tag1Id, tag2Id };

        var tag1 = CreateTestTag(tag1Id, "Tag 1", "tag-1");
        var tag2 = CreateTestTag(tag2Id, "Tag 2", "tag-2");

        var command = CreateTestCommand(tagIds: tagIds);

        Post? capturedPost = null;

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        _postRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()))
            .Callback<Post, CancellationToken>((post, _) => capturedPost = post)
            .ReturnsAsync((Post post, CancellationToken _) => post);

        _tagRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<TagsByIdsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostTag> { tag1, tag2 });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        capturedPost.ShouldNotBeNull();
        capturedPost!.TagAssignments.Count().ShouldBe(2);
        capturedPost.TagAssignments.Select(ta => ta.TagId).ShouldContain(tag1Id);
        capturedPost.TagAssignments.Select(ta => ta.TagId).ShouldContain(tag2Id);

        // Verify tag counts were incremented
        tag1.PostCount.ShouldBe(1);
        tag2.PostCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_WithEmptyTagIds_ShouldNotQueryTags()
    {
        // Arrange
        var command = CreateTestCommand(tagIds: new List<Guid>());

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        _postRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _tagRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<TagsByIdsSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithNullTagIds_ShouldNotQueryTags()
    {
        // Arrange
        var command = CreateTestCommand(tagIds: null);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        _postRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _tagRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<TagsByIdsSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var existingPost = Post.Create("Existing Post", "test-post-title", Guid.NewGuid(), TestTenantId);
        var command = CreateTestCommand(slug: "test-post-title");

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPost);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("NOIR-BLOG-001");
        result.Error.Message.ShouldContain("test-post-title");

        _postRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Validation Scenarios

    [Fact]
    public async Task Handle_WithNullUserId_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand(userId: null);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("NOIR-BLOG-002");
        result.Error.Message.ShouldContain("Invalid author ID");
    }

    [Fact]
    public async Task Handle_WithEmptyUserId_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand(userId: "");

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("NOIR-BLOG-002");
    }

    [Fact]
    public async Task Handle_WithInvalidGuidUserId_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand(userId: "not-a-valid-guid");

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("NOIR-BLOG-002");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var command = CreateTestCommand();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        _postRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _postRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), token),
            Times.Once);
        _postRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Post>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNormalizeSlugToLowerCase()
    {
        // Arrange
        var command = CreateTestCommand(slug: "MY-UPPERCASE-SLUG");

        Post? capturedPost = null;

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        _postRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()))
            .Callback<Post, CancellationToken>((post, _) => capturedPost = post)
            .ReturnsAsync((Post post, CancellationToken _) => post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        capturedPost.ShouldNotBeNull();
        capturedPost!.Slug.ShouldBe("my-uppercase-slug");
    }

    [Fact]
    public async Task Handle_ShouldSetAuthorIdFromUserId()
    {
        // Arrange
        var authorGuid = Guid.Parse(TestUserId);
        var command = CreateTestCommand(userId: TestUserId);

        Post? capturedPost = null;

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        _postRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()))
            .Callback<Post, CancellationToken>((post, _) => capturedPost = post)
            .ReturnsAsync((Post post, CancellationToken _) => post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        capturedPost.ShouldNotBeNull();
        capturedPost!.AuthorId.ShouldBe(authorGuid);
    }

    [Fact]
    public async Task Handle_WhenFeaturedImageIdAndUrlBothProvided_ShouldPreferImageId()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var command = CreateTestCommand(
            featuredImageId: imageId,
            featuredImageUrl: "https://example.com/image.jpg",
            featuredImageAlt: "Alt text");

        Post? capturedPost = null;

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        _postRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()))
            .Callback<Post, CancellationToken>((post, _) => capturedPost = post)
            .ReturnsAsync((Post post, CancellationToken _) => post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        capturedPost.ShouldNotBeNull();
        capturedPost!.FeaturedImageId.ShouldBe(imageId);
        // When using SetFeaturedImage with ID, URL is cleared
        capturedPost.FeaturedImageUrl.ShouldBeNull();
    }

    #endregion
}
