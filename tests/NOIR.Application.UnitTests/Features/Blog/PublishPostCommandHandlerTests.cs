using NOIR.Application.Features.Blog.Commands.PublishPost;
using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Specifications;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for PublishPostCommandHandler.
/// Tests post publishing and scheduling scenarios with mocked dependencies.
/// </summary>
public class PublishPostCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Post, Guid>> _postRepositoryMock;
    private readonly Mock<IRepository<PostCategory, Guid>> _categoryRepositoryMock;
    private readonly Mock<IRepository<PostTag, Guid>> _tagRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly PublishPostCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "550e8400-e29b-41d4-a716-446655440000";

    public PublishPostCommandHandlerTests()
    {
        _postRepositoryMock = new Mock<IRepository<Post, Guid>>();
        _categoryRepositoryMock = new Mock<IRepository<PostCategory, Guid>>();
        _tagRepositoryMock = new Mock<IRepository<PostTag, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new PublishPostCommandHandler(
            _postRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _tagRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static PublishPostCommand CreateTestCommand(
        Guid? id = null,
        DateTimeOffset? scheduledPublishAt = null,
        string? postTitle = null,
        string? userId = TestUserId)
    {
        return new PublishPostCommand(id ?? Guid.NewGuid(), scheduledPublishAt, postTitle)
        { UserId = userId };
    }

    private static Post CreateTestPost(
        Guid? id = null,
        string title = "Test Post",
        string slug = "test-post",
        PostStatus status = PostStatus.Draft,
        string? tenantId = TestTenantId)
    {
        var post = Post.Create(title, slug, Guid.Parse(TestUserId), tenantId);
        if (id.HasValue)
        {
            typeof(Post).GetProperty("Id")?.SetValue(post, id.Value);
        }
        return post;
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

    private static PostTag CreateTestTag(Guid? id = null, string name = "Test Tag", string slug = "test-tag")
    {
        var tag = PostTag.Create(name, slug, "Test description", "#3B82F6", TestTenantId);
        if (id.HasValue)
        {
            typeof(PostTag).GetProperty("Id")?.SetValue(tag, id.Value);
        }
        return tag;
    }

    #endregion

    #region Immediate Publish Scenarios

    [Fact]
    public async Task Handle_WithNoScheduledDate_ShouldPublishImmediately()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId);
        var command = CreateTestCommand(id: postId, scheduledPublishAt: null);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Status.Should().Be(PostStatus.Published);
        post.Status.Should().Be(PostStatus.Published);
        post.PublishedAt.Should().NotBeNull();
        post.PublishedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        post.ScheduledPublishAt.Should().BeNull();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_FromDraftStatus_ShouldPublish()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId, status: PostStatus.Draft);
        var command = CreateTestCommand(id: postId);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        post.Status.Should().Be(PostStatus.Published);
    }

    [Fact]
    public async Task Handle_FromScheduledStatus_ShouldPublishImmediately()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId);
        post.Schedule(DateTimeOffset.UtcNow.AddDays(7)); // Originally scheduled for future
        var command = CreateTestCommand(id: postId, scheduledPublishAt: null); // Publish now

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        post.Status.Should().Be(PostStatus.Published);
        post.ScheduledPublishAt.Should().BeNull();
        post.PublishedAt.Should().NotBeNull();
    }

    #endregion

    #region Schedule Publish Scenarios

    [Fact]
    public async Task Handle_WithFutureScheduledDate_ShouldSchedulePost()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId);
        var futureDate = DateTimeOffset.UtcNow.AddDays(7);
        var command = CreateTestCommand(id: postId, scheduledPublishAt: futureDate);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PostStatus.Scheduled);
        post.Status.Should().Be(PostStatus.Scheduled);
        post.ScheduledPublishAt.Should().Be(futureDate);
        post.PublishedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithScheduledDate_ShouldClearPublishedAt()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId);
        post.Publish(); // Already published
        var futureDate = DateTimeOffset.UtcNow.AddDays(7);
        var command = CreateTestCommand(id: postId, scheduledPublishAt: futureDate);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        post.Status.Should().Be(PostStatus.Scheduled);
        post.ScheduledPublishAt.Should().Be(futureDate);
        post.PublishedAt.Should().BeNull(); // Cleared when scheduling
    }

    [Fact]
    public async Task Handle_WithPastScheduledDate_ShouldReturnValidationError()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId);
        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);
        var command = CreateTestCommand(id: postId, scheduledPublishAt: pastDate);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-BLOG-004");
        result.Error.Message.Should().Contain("future");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithCurrentTimeScheduledDate_ShouldReturnValidationError()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId);
        // Use a time slightly in the past to ensure it's not in the future
        var currentDate = DateTimeOffset.UtcNow.AddSeconds(-1);
        var command = CreateTestCommand(id: postId, scheduledPublishAt: currentDate);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-BLOG-004");
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
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-BLOG-003");
        result.Error.Message.Should().Contain(postId.ToString());

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region DTO Mapping Scenarios

    [Fact]
    public async Task Handle_WithCategory_ShouldIncludeCategoryNameInDto()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var post = CreateTestPost(postId);
        post.SetCategory(categoryId);
        var category = CreateTestCategory(categoryId, "Technology");
        var command = CreateTestCommand(id: postId);

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
        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryId.Should().Be(categoryId);
        result.Value.CategoryName.Should().Be("Technology");
    }

    [Fact]
    public async Task Handle_WithNoCategory_ShouldNotQueryCategory()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId);
        var command = CreateTestCommand(id: postId);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryId.Should().BeNull();
        result.Value.CategoryName.Should().BeNull();

        _categoryRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CategoryByIdSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithTags_ShouldIncludeTagsInDto()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var tag1Id = Guid.NewGuid();
        var tag2Id = Guid.NewGuid();
        var post = CreateTestPost(postId);
        var tag1 = CreateTestTag(tag1Id, "Tag 1", "tag-1");
        var tag2 = CreateTestTag(tag2Id, "Tag 2", "tag-2");

        // Create tag assignments with navigation properties
        var assignment1 = PostTagAssignment.Create(postId, tag1Id, TestTenantId);
        var assignment2 = PostTagAssignment.Create(postId, tag2Id, TestTenantId);

        // Use reflection to set Tag navigation property
        typeof(PostTagAssignment).GetProperty("Tag")?.SetValue(assignment1, tag1);
        typeof(PostTagAssignment).GetProperty("Tag")?.SetValue(assignment2, tag2);

        post.TagAssignments.Add(assignment1);
        post.TagAssignments.Add(assignment2);

        var command = CreateTestCommand(id: postId);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tags.Should().HaveCount(2);
        result.Value.Tags.Select(t => t.Name).Should().Contain("Tag 1");
        result.Value.Tags.Select(t => t.Name).Should().Contain("Tag 2");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId);
        var command = CreateTestCommand(id: postId);
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
    public async Task Handle_ShouldReturnDtoWithAllPostFields()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId, title: "My Post Title", slug: "my-post-title");
        post.UpdateContent("My Post Title", "my-post-title", "Excerpt here", "{}", "<p>Content</p>");
        post.UpdateSeo("Meta Title", "Meta Description", "https://example.com", true);

        var command = CreateTestCommand(id: postId);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(postId);
        result.Value.Title.Should().Be("My Post Title");
        result.Value.Slug.Should().Be("my-post-title");
        result.Value.Excerpt.Should().Be("Excerpt here");
        result.Value.ContentJson.Should().Be("{}");
        result.Value.ContentHtml.Should().Be("<p>Content</p>");
        result.Value.MetaTitle.Should().Be("Meta Title");
        result.Value.MetaDescription.Should().Be("Meta Description");
        result.Value.CanonicalUrl.Should().Be("https://example.com");
        result.Value.AllowIndexing.Should().BeTrue();
        result.Value.Status.Should().Be(PostStatus.Published);
    }

    [Fact]
    public async Task Handle_Publish_ShouldSetPublishedAtToUtcNow()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId);
        var command = CreateTestCommand(id: postId);
        var beforeHandle = DateTimeOffset.UtcNow;

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        var afterHandle = DateTimeOffset.UtcNow;

        // Assert
        result.IsSuccess.Should().BeTrue();
        post.PublishedAt.Should().NotBeNull();
        post.PublishedAt.Should().BeOnOrAfter(beforeHandle);
        post.PublishedAt.Should().BeOnOrBefore(afterHandle);
    }

    [Fact]
    public async Task Handle_Schedule_ShouldSetScheduledPublishAtExactly()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId);
        var scheduleTime = DateTimeOffset.UtcNow.AddHours(48).AddMinutes(30);
        var command = CreateTestCommand(id: postId, scheduledPublishAt: scheduleTime);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        post.ScheduledPublishAt.Should().Be(scheduleTime);
        result.Value.ScheduledPublishAt.Should().Be(scheduleTime);
    }

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ShouldReturnNullCategoryName()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var post = CreateTestPost(postId);
        post.SetCategory(categoryId);
        var command = CreateTestCommand(id: postId);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CategoryByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryId.Should().Be(categoryId);
        result.Value.CategoryName.Should().BeNull();
    }

    #endregion
}
