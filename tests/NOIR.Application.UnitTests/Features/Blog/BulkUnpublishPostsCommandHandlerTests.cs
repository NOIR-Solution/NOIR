using NOIR.Application.Common.DTOs;
using NOIR.Application.Features.Blog.Commands.BulkUnpublishPosts;
using NOIR.Application.Features.Blog.Specifications;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for BulkUnpublishPostsCommandHandler.
/// Tests bulk post unpublish scenarios with mocked dependencies.
/// </summary>
public class BulkUnpublishPostsCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Post, Guid>> _postRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly BulkUnpublishPostsCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestAuthorId = Guid.NewGuid();

    public BulkUnpublishPostsCommandHandlerTests()
    {
        _postRepositoryMock = new Mock<IRepository<Post, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new BulkUnpublishPostsCommandHandler(
            _postRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static BulkUnpublishPostsCommand CreateTestCommand(List<Guid>? postIds = null)
    {
        return new BulkUnpublishPostsCommand(postIds ?? new List<Guid> { Guid.NewGuid() });
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
    public async Task Handle_WithPublishedPosts_ShouldUnpublishAll()
    {
        // Arrange
        var postId1 = Guid.NewGuid();
        var postId2 = Guid.NewGuid();
        var postId3 = Guid.NewGuid();
        var postIds = new List<Guid> { postId1, postId2, postId3 };

        var post1 = CreateTestPost(postId1, "Post 1", "post-1", PostStatus.Published);
        var post2 = CreateTestPost(postId2, "Post 2", "post-2", PostStatus.Published);
        var post3 = CreateTestPost(postId3, "Post 3", "post-3", PostStatus.Published);

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

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Partial Success Scenarios

    [Fact]
    public async Task Handle_WithMixedStatuses_ShouldUnpublishOnlyPublished()
    {
        // Arrange
        var publishedId = Guid.NewGuid();
        var draftId = Guid.NewGuid();
        var postIds = new List<Guid> { publishedId, draftId };

        var publishedPost = CreateTestPost(publishedId, "Published Post", "published-post", PostStatus.Published);
        var draftPost = CreateTestPost(draftId, "Draft Post", "draft-post", PostStatus.Draft);

        var command = CreateTestCommand(postIds);

        _postRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PostsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post> { publishedPost, draftPost });

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
        result.Value.Errors[0].EntityId.ShouldBe(draftId);
        result.Value.Errors[0].Message.ShouldContain("not in Published status");
    }

    [Fact]
    public async Task Handle_WithNonExistentIds_ShouldReturnErrors()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();
        var postIds = new List<Guid> { existingId, nonExistentId };

        var existingPost = CreateTestPost(existingId, "Published Post", "published-post", PostStatus.Published);
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
    }

    #endregion
}
