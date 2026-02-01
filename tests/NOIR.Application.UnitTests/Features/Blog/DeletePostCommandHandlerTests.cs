using NOIR.Application.Features.Blog.Commands.DeletePost;
using NOIR.Application.Features.Blog.Specifications;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for DeletePostCommandHandler.
/// Tests post deletion scenarios with mocked dependencies.
/// </summary>
public class DeletePostCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Post, Guid>> _postRepositoryMock;
    private readonly Mock<IRepository<PostTag, Guid>> _tagRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly DeletePostCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "550e8400-e29b-41d4-a716-446655440000";

    public DeletePostCommandHandlerTests()
    {
        _postRepositoryMock = new Mock<IRepository<Post, Guid>>();
        _tagRepositoryMock = new Mock<IRepository<PostTag, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeletePostCommandHandler(
            _postRepositoryMock.Object,
            _tagRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static DeletePostCommand CreateTestCommand(
        Guid? id = null,
        string? postTitle = null,
        string? userId = TestUserId)
    {
        return new DeletePostCommand(id ?? Guid.NewGuid(), postTitle)
        { UserId = userId };
    }

    private static Post CreateTestPost(
        Guid? id = null,
        string title = "Test Post",
        string slug = "test-post",
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

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidPostId_ShouldSucceed()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId);
        var command = CreateTestCommand(id: postId, postTitle: "Test Post");

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
        result.Value.Should().BeTrue();

        _postRepositoryMock.Verify(
            x => x.Remove(post),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldClearTagAssignments()
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
        post.TagAssignments.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithTagAssignments_ShouldDecrementTagCounts()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var tag1Id = Guid.NewGuid();
        var tag2Id = Guid.NewGuid();
        var post = CreateTestPost(postId);
        var tag1 = CreateTestTag(tag1Id, "Tag 1", "tag-1");
        var tag2 = CreateTestTag(tag2Id, "Tag 2", "tag-2");

        // Simulate initial tag counts and assignments
        tag1.IncrementPostCount(); // Count = 1
        tag2.IncrementPostCount(); // Count = 1
        tag2.IncrementPostCount(); // Count = 2 (used by another post)

        var assignment1 = PostTagAssignment.Create(postId, tag1Id, TestTenantId);
        var assignment2 = PostTagAssignment.Create(postId, tag2Id, TestTenantId);
        post.TagAssignments.Add(assignment1);
        post.TagAssignments.Add(assignment2);

        var command = CreateTestCommand(id: postId);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        // Batch fetch all tags in single query (fixes N+1)
        _tagRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<PostTag>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostTag> { tag1, tag2 });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tag1.PostCount.Should().Be(0); // Decremented from 1 to 0
        tag2.PostCount.Should().Be(1); // Decremented from 2 to 1
    }

    [Fact]
    public async Task Handle_WithNoTagAssignments_ShouldNotQueryTags()
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
        // No tags to query since post has no tag assignments
        _tagRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ISpecification<PostTag>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTagNotFound_ShouldStillSucceed()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var post = CreateTestPost(postId);

        var assignment = PostTagAssignment.Create(postId, tagId, TestTenantId);
        post.TagAssignments.Add(assignment);

        var command = CreateTestCommand(id: postId);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        // Tag not found (already deleted) - batch query returns empty list
        _tagRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<PostTag>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostTag>());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        post.TagAssignments.Should().BeEmpty();
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

        _postRepositoryMock.Verify(
            x => x.Remove(It.IsAny<Post>()),
            Times.Never);
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
    public async Task Handle_ShouldCallRemoveOnRepository()
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
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _postRepositoryMock.Verify(
            x => x.Remove(post),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleTagAssignments_ShouldDecrementAllTagCounts()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId);

        // Create 3 tags with different initial counts
        var tags = new List<PostTag>();
        for (int i = 0; i < 3; i++)
        {
            var tagId = Guid.NewGuid();
            var tag = CreateTestTag(tagId, $"Tag {i}", $"tag-{i}");
            // Set different initial counts
            for (int j = 0; j <= i; j++)
            {
                tag.IncrementPostCount();
            }
            tags.Add(tag);

            var assignment = PostTagAssignment.Create(postId, tagId, TestTenantId);
            post.TagAssignments.Add(assignment);
        }

        var command = CreateTestCommand(id: postId);

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        // Batch fetch all tags in single query (fixes N+1)
        _tagRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<PostTag>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tags[0].PostCount.Should().Be(0); // Was 1, now 0
        tags[1].PostCount.Should().Be(1); // Was 2, now 1
        tags[2].PostCount.Should().Be(2); // Was 3, now 2
    }

    [Fact]
    public async Task Handle_ShouldSoftDeletePost()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = CreateTestPost(postId);
        var command = CreateTestCommand(id: postId);

        Post? removedPost = null;

        _postRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PostByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _postRepositoryMock
            .Setup(x => x.Remove(It.IsAny<Post>()))
            .Callback<Post>(p => removedPost = p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        removedPost.Should().Be(post);
        // Note: The actual soft delete is handled by the interceptor,
        // this test just verifies Remove is called on the correct entity
    }

    #endregion
}
