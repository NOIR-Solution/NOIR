using NOIR.Application.Features.Blog.Commands.UnpublishPost;
using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Specifications;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for UnpublishPostCommandHandler.
/// Tests unpublishing (reverting to draft) blog posts.
/// </summary>
public class UnpublishPostCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Post, Guid>> _postRepoMock;
    private readonly Mock<IRepository<PostCategory, Guid>> _categoryRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UnpublishPostCommandHandler _handler;

    public UnpublishPostCommandHandlerTests()
    {
        _postRepoMock = new Mock<IRepository<Post, Guid>>();
        _categoryRepoMock = new Mock<IRepository<PostCategory, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UnpublishPostCommandHandler(
            _postRepoMock.Object,
            _categoryRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    private static Post CreateTestPost(PostStatus status = PostStatus.Published, Guid? categoryId = null)
    {
        var post = Post.Create(
            title: "Test Post",
            slug: "test-post",
            authorId: Guid.NewGuid(),
            tenantId: "test-tenant");

        if (status == PostStatus.Published)
        {
            post.Publish();
        }
        else if (status == PostStatus.Scheduled)
        {
            post.Schedule(DateTimeOffset.UtcNow.AddDays(1));
        }

        if (categoryId.HasValue)
        {
            post.SetCategory(categoryId.Value);
        }

        return post;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_PublishedPost_ShouldUnpublish()
    {
        // Arrange
        var post = CreateTestPost(PostStatus.Published);
        var postId = post.Id;

        _postRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var command = new UnpublishPostCommand(postId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PostStatus.Draft);
        post.Status.Should().Be(PostStatus.Draft);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ScheduledPost_ShouldUnpublishAndClearSchedule()
    {
        // Arrange
        var post = CreateTestPost(PostStatus.Scheduled);
        var postId = post.Id;

        _postRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var command = new UnpublishPostCommand(postId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PostStatus.Draft);
        result.Value.ScheduledPublishAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithCategory_ShouldReturnCategoryInfo()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = PostCategory.Create("Test Category", "test-category", tenantId: "test-tenant");

        var post = CreateTestPost(PostStatus.Published, categoryId);
        var postId = post.Id;

        _postRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _categoryRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CategoryByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var command = new UnpublishPostCommand(postId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryName.Should().Be("Test Category");
        result.Value.CategorySlug.Should().Be("test-category");
    }

    [Fact]
    public async Task Handle_DraftPost_ShouldStillSucceed()
    {
        // Arrange - already draft
        var post = CreateTestPost(PostStatus.Draft);
        var postId = post.Id;

        _postRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var command = new UnpublishPostCommand(postId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PostStatus.Draft);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_PostNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var postId = Guid.NewGuid();

        _postRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        var command = new UnpublishPostCommand(postId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-BLOG-003");
    }

    #endregion
}
