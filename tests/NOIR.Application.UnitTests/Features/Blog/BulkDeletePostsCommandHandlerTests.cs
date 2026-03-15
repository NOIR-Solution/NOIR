using NOIR.Application.Common.DTOs;
using NOIR.Application.Features.Blog.Commands.BulkDeletePosts;
using NOIR.Application.Features.Blog.Specifications;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for BulkDeletePostsCommandHandler.
/// Tests bulk post soft-delete scenarios with mocked dependencies.
/// </summary>
public class BulkDeletePostsCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Post, Guid>> _postRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly BulkDeletePostsCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestAuthorId = Guid.NewGuid();

    public BulkDeletePostsCommandHandlerTests()
    {
        _postRepositoryMock = new Mock<IRepository<Post, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new BulkDeletePostsCommandHandler(
            _postRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static BulkDeletePostsCommand CreateTestCommand(List<Guid>? postIds = null)
    {
        return new BulkDeletePostsCommand(postIds ?? new List<Guid> { Guid.NewGuid() });
    }

    private static Post CreateTestPost(
        Guid? id = null,
        string title = "Test Post",
        string slug = "test-post",
        PostStatus status = PostStatus.Draft)
    {
        var post = Post.Create(title, slug, TestAuthorId, TestTenantId);

        if (id.HasValue)
        {
            typeof(Post).GetProperty("Id")!.SetValue(post, id.Value);
        }

        if (status == PostStatus.Published)
        {
            post.Publish();
        }

        return post;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidPosts_ShouldDeleteAll()
    {
        // Arrange
        var postId1 = Guid.NewGuid();
        var postId2 = Guid.NewGuid();
        var postId3 = Guid.NewGuid();
        var postIds = new List<Guid> { postId1, postId2, postId3 };

        var post1 = CreateTestPost(postId1, "Post 1", "post-1", PostStatus.Draft);
        var post2 = CreateTestPost(postId2, "Post 2", "post-2", PostStatus.Published);
        var post3 = CreateTestPost(postId3, "Post 3", "post-3", PostStatus.Draft);

        var command = CreateTestCommand(postIds);

        _postRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PostsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post> { post1, post2, post3 });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(3);
        result.Value.Failed.ShouldBe(0);
        result.Value.Errors.ShouldBeEmpty();

        _postRepositoryMock.Verify(
            x => x.Remove(It.IsAny<Post>()),
            Times.Exactly(3));

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Partial Success Scenarios

    [Fact]
    public async Task Handle_WithNonExistentIds_ShouldReturnErrors()
    {
        // Arrange
        var nonExistentId1 = Guid.NewGuid();
        var nonExistentId2 = Guid.NewGuid();
        var postIds = new List<Guid> { nonExistentId1, nonExistentId2 };

        var command = CreateTestCommand(postIds);

        _postRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PostsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(0);
        result.Value.Failed.ShouldBe(2);
        result.Value.Errors.Count().ShouldBe(2);
        result.Value.Errors.ShouldAllBe(e => e.Message.Contains("not found"));
    }

    [Fact]
    public async Task Handle_WithMixedExistence_ShouldDeleteFoundAndErrorMissing()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();
        var postIds = new List<Guid> { existingId, nonExistentId };

        var existingPost = CreateTestPost(existingId, "Existing Post", "existing-post", PostStatus.Draft);
        var command = CreateTestCommand(postIds);

        _postRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PostsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post> { existingPost });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(1);
        result.Value.Failed.ShouldBe(1);
        result.Value.Errors.Count().ShouldBe(1);
        result.Value.Errors[0].EntityId.ShouldBe(nonExistentId);
        result.Value.Errors[0].Message.ShouldContain("not found");

        _postRepositoryMock.Verify(
            x => x.Remove(existingPost),
            Times.Once);
    }

    #endregion
}
