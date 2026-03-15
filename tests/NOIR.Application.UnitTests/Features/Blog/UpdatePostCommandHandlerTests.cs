using NOIR.Application.Features.Blog.Commands.UpdatePost;
using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Services;
using NOIR.Application.Features.Blog.Specifications;
using NOIR.Domain.ValueObjects;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for UpdatePostCommandHandler.
/// Tests post update scenarios with mocked dependencies.
/// </summary>
public class UpdatePostCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Post, Guid>> _postRepositoryMock;
    private readonly Mock<IRepository<PostTag, Guid>> _tagRepositoryMock;
    private readonly Mock<IRepository<PostCategory, Guid>> _categoryRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IContentAnalyzer> _contentAnalyzerMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly UpdatePostCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "550e8400-e29b-41d4-a716-446655440000";

    public UpdatePostCommandHandlerTests()
    {
        _postRepositoryMock = new Mock<IRepository<Post, Guid>>();
        _tagRepositoryMock = new Mock<IRepository<PostTag, Guid>>();
        _categoryRepositoryMock = new Mock<IRepository<PostCategory, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _contentAnalyzerMock = new Mock<IContentAnalyzer>();

        // Setup default current user
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        // Setup default content analyzer (returns empty metadata)
        _contentAnalyzerMock
            .Setup(x => x.Analyze(It.IsAny<string?>()))
            .Returns(new ContentMetadata());

        _handler = new UpdatePostCommandHandler(
            _postRepositoryMock.Object,
            _tagRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _contentAnalyzerMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static UpdatePostCommand CreateTestCommand(
        Guid? id = null,
        string title = "Updated Post Title",
        string slug = "updated-post-title",
        string? excerpt = "Updated excerpt",
        string? contentJson = "{\"blocks\":[]}",
        string? contentHtml = "<p>Updated content</p>",
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
        return new UpdatePostCommand(
            id ?? Guid.NewGuid(),
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

    private static Post CreateTestPost(
        Guid? id = null,
        string title = "Original Title",
        string slug = "original-slug",
        string? tenantId = TestTenantId)
    {
        var post = Post.Create(title, slug, Guid.Parse(TestUserId), tenantId);
        if (id.HasValue)
        {
            typeof(Post).GetProperty("Id")?.SetValue(post, id.Value);
        }
        return post;
    }

    private static PostTag CreateTestTag(Guid? id = null, string name = "Test Tag", string slug = "test-tag")
    {
        var tag = PostTag.Create(name, slug, "Test description", "#3B82F6", TestTenantId);
        if (id.HasValue)
        {
            typeof(PostTag).GetProperty("Id")?.SetValue(tag, id.Value);
        }
        return tag;
    }

    private static PostCategory CreateTestCategory(Guid? id = null, string name = "Test Category")
    {
        var category = PostCategory.Create(name, name.ToLowerInvariant().Replace(" ", "-"), null, null, TestTenantId);
        if (id.HasValue)
        {
            typeof(PostCategory).GetProperty("Id")?.SetValue(category, id.Value);
        }
        return category;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSucceed()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId);
        var command = CreateTestCommand(id: postId, slug: "original-slug");

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBe(postId);
        result.Value.Title.ShouldBe(command.Title);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithContentUpdate_ShouldUpdateAllContentFields()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId);
        var command = CreateTestCommand(
            id: postId,
            title: "New Title",
            slug: "original-slug",
            excerpt: "New excerpt",
            contentJson: "{\"new\":\"content\"}",
            contentHtml: "<p>New HTML</p>");

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        post.Title.ShouldBe("New Title");
        post.Excerpt.ShouldBe("New excerpt");
        post.ContentJson.ShouldBe("{\"new\":\"content\"}");
        post.ContentHtml.ShouldBe("<p>New HTML</p>");
    }

    [Fact]
    public async Task Handle_WithNewSlug_ShouldCheckSlugUniqueness()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId, slug: "original-slug");
        var command = CreateTestCommand(id: postId, slug: "new-unique-slug");

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        post.Slug.ShouldBe("new-unique-slug");

        _postRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSameSlug_ShouldNotCheckSlugUniqueness()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId, slug: "same-slug");
        var command = CreateTestCommand(id: postId, slug: "same-slug");

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);

        // Should not check for slug uniqueness when slug hasn't changed
        _postRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithFeaturedImageId_ShouldUpdateFeaturedImageId()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var post = CreateTestPost(postId, slug: "same-slug");
        var command = CreateTestCommand(
            id: postId,
            slug: "same-slug",
            featuredImageId: imageId,
            featuredImageAlt: "New alt text");

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        post.FeaturedImageId.ShouldBe(imageId);
        post.FeaturedImageAlt.ShouldBe("New alt text");
    }

    [Fact]
    public async Task Handle_WithFeaturedImageUrl_ShouldUpdateFeaturedImageUrl()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId, slug: "same-slug");
        var command = CreateTestCommand(
            id: postId,
            slug: "same-slug",
            featuredImageUrl: "https://example.com/new-image.jpg",
            featuredImageAlt: "New alt text");

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        post.FeaturedImageUrl.ShouldBe("https://example.com/new-image.jpg");
        post.FeaturedImageAlt.ShouldBe("New alt text");
    }

    [Fact]
    public async Task Handle_WithSeoMetadata_ShouldUpdateSeoFields()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId, slug: "same-slug");
        var command = CreateTestCommand(
            id: postId,
            slug: "same-slug",
            metaTitle: "New SEO Title",
            metaDescription: "New SEO Description",
            canonicalUrl: "https://example.com/new-canonical",
            allowIndexing: false);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        post.MetaTitle.ShouldBe("New SEO Title");
        post.MetaDescription.ShouldBe("New SEO Description");
        post.CanonicalUrl.ShouldBe("https://example.com/new-canonical");
        post.AllowIndexing.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_WithCategoryId_ShouldUpdateCategory()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var post = CreateTestPost(postId, slug: "same-slug");
        var category = CreateTestCategory(categoryId);
        var command = CreateTestCommand(id: postId, slug: "same-slug", categoryId: categoryId);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CategoryByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        post.CategoryId.ShouldBe(categoryId);
        result.Value.CategoryName.ShouldBe(category.Name);
    }

    #endregion

    #region Tag Management Scenarios

    [Fact]
    public async Task Handle_WithNewTags_ShouldAddTagAssignments()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var tag1Id = Guid.NewGuid();
        var tag2Id = Guid.NewGuid();
        var post = CreateTestPost(postId, slug: "same-slug");
        var tag1 = CreateTestTag(tag1Id, "Tag 1", "tag-1");
        var tag2 = CreateTestTag(tag2Id, "Tag 2", "tag-2");
        var command = CreateTestCommand(
            id: postId,
            slug: "same-slug",
            tagIds: new List<Guid> { tag1Id, tag2Id });

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _tagRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<PostTag>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostTag> { tag1, tag2 });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        post.TagAssignments.Count().ShouldBe(2);
        tag1.PostCount.ShouldBe(1);
        tag2.PostCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_WithRemovedTags_ShouldRemoveTagAssignmentsAndDecrementCounts()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var tag1Id = Guid.NewGuid();
        var tag2Id = Guid.NewGuid();
        var post = CreateTestPost(postId, slug: "same-slug");
        var tag1 = CreateTestTag(tag1Id, "Tag 1", "tag-1");
        var tag2 = CreateTestTag(tag2Id, "Tag 2", "tag-2");

        // Simulate existing tag assignments
        tag1.IncrementPostCount();
        tag2.IncrementPostCount();
        var assignment1 = PostTagAssignment.Create(postId, tag1Id, TestTenantId);
        var assignment2 = PostTagAssignment.Create(postId, tag2Id, TestTenantId);
        post.TagAssignments.Add(assignment1);
        post.TagAssignments.Add(assignment2);

        // Only keep tag1, remove tag2
        var command = CreateTestCommand(
            id: postId,
            slug: "same-slug",
            tagIds: new List<Guid> { tag1Id });

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        // Batch fetch tags to remove (tag2 only since tag1 is being kept)
        _tagRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<PostTag>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostTag> { tag2 });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        post.TagAssignments.Count().ShouldBe(1);
        post.TagAssignments.First().TagId.ShouldBe(tag1Id);
        tag2.PostCount.ShouldBe(0); // Decremented
    }

    [Fact]
    public async Task Handle_WithEmptyTagIds_ShouldRemoveAllTagAssignments()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var tag1Id = Guid.NewGuid();
        var post = CreateTestPost(postId, slug: "same-slug");
        var tag1 = CreateTestTag(tag1Id, "Tag 1", "tag-1");

        tag1.IncrementPostCount();
        var assignment1 = PostTagAssignment.Create(postId, tag1Id, TestTenantId);
        post.TagAssignments.Add(assignment1);

        var command = CreateTestCommand(
            id: postId,
            slug: "same-slug",
            tagIds: new List<Guid>());

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        // Batch fetch tags to remove (all tags since empty tagIds)
        _tagRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<PostTag>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostTag> { tag1 });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        post.TagAssignments.ShouldBeEmpty();
        tag1.PostCount.ShouldBe(0);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenPostNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var command = CreateTestCommand(id: postId);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("NOIR-BLOG-003");
        result.Error.Message.ShouldContain(postId.ToString());

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenNewSlugAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var existingPostId = Guid.NewGuid();
        var post = CreateTestPost(postId, slug: "original-slug");
        var existingPost = CreateTestPost(existingPostId, slug: "conflicting-slug");
        var command = CreateTestCommand(id: postId, slug: "conflicting-slug");

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPost);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("NOIR-BLOG-001");
        result.Error.Message.ShouldContain("conflicting-slug");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId, slug: "same-slug");
        var command = CreateTestCommand(id: postId, slug: "same-slug");
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _postRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNormalizeSlugToLowerCase()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId, slug: "original-slug");
        var command = CreateTestCommand(id: postId, slug: "NEW-UPPERCASE-SLUG");

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostSlugExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        post.Slug.ShouldBe("new-uppercase-slug");
    }

    [Fact]
    public async Task Handle_WhenFeaturedImageIdProvided_ShouldPreferOverUrl()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var post = CreateTestPost(postId, slug: "same-slug");
        var command = CreateTestCommand(
            id: postId,
            slug: "same-slug",
            featuredImageId: imageId,
            featuredImageUrl: "https://example.com/should-be-ignored.jpg");

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        post.FeaturedImageId.ShouldBe(imageId);
        // URL should be null when using ImageId
        post.FeaturedImageUrl.ShouldBeNull();
    }

    #endregion
}
