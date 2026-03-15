using NOIR.Application.Features.Blog.Commands.DeleteTag;
using NOIR.Application.Features.Blog.Specifications;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for DeleteTagCommandHandler.
/// Tests tag deletion scenarios with mocked dependencies.
/// </summary>
public class DeleteTagCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PostTag, Guid>> _tagRepositoryMock;
    private readonly Mock<IRepository<Post, Guid>> _postRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly DeleteTagCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public DeleteTagCommandHandlerTests()
    {
        _tagRepositoryMock = new Mock<IRepository<PostTag, Guid>>();
        _postRepositoryMock = new Mock<IRepository<Post, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeleteTagCommandHandler(
            _tagRepositoryMock.Object,
            _postRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static PostTag CreateTestTag(
        Guid? id = null,
        string name = "Test Tag",
        string slug = "test-tag",
        string? tenantId = TestTenantId)
    {
        var tag = PostTag.Create(name, slug, null, null, tenantId);
        if (id.HasValue)
        {
            typeof(PostTag).GetProperty("Id")!.SetValue(tag, id.Value);
        }
        return tag;
    }

    private static Post CreateTestPost(
        Guid? id = null,
        string title = "Test Post",
        string slug = "test-post",
        string? tenantId = TestTenantId)
    {
        var post = Post.Create(
            title,
            slug,
            authorId: Guid.NewGuid(),
            tenantId: tenantId);

        if (id.HasValue)
        {
            typeof(Post).GetProperty("Id")!.SetValue(post, id.Value);
        }
        return post;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenTagExists_ShouldDeleteSuccessfully()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var existingTag = CreateTestTag(id: tagId);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        _postRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PostsWithTagForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteTagCommand(tagId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBe(true);

        _tagRepositoryMock.Verify(
            x => x.Remove(existingTag),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTagHasPosts_ShouldRemoveTagFromPosts()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var existingTag = CreateTestTag(id: tagId);

        var postId1 = Guid.NewGuid();
        var postId2 = Guid.NewGuid();

        var post1 = CreateTestPost(id: postId1);
        var post2 = CreateTestPost(id: postId2);

        // Add tag assignments to posts
        var assignment1 = PostTagAssignment.Create(postId1, tagId, TestTenantId);
        var assignment2 = PostTagAssignment.Create(postId2, tagId, TestTenantId);

        post1.TagAssignments.Add(assignment1);
        post2.TagAssignments.Add(assignment2);

        var postsWithTag = new List<Post> { post1, post2 };

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        _postRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PostsWithTagForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(postsWithTag);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteTagCommand(tagId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);

        // Verify tag assignments were removed from posts
        post1.TagAssignments.ShouldBeEmpty();
        post2.TagAssignments.ShouldBeEmpty();

        _tagRepositoryMock.Verify(
            x => x.Remove(existingTag),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithTagName_ShouldDeleteSuccessfully()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        const string tagName = "Featured Tag";
        var existingTag = CreateTestTag(id: tagId, name: tagName);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        _postRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PostsWithTagForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteTagCommand(tagId, tagName);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBe(true);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenTagNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var tagId = Guid.NewGuid();

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostTag?)null);

        var command = new DeleteTagCommand(tagId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("NOIR-BLOG-012");

        _tagRepositoryMock.Verify(
            x => x.Remove(It.IsAny<PostTag>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ShouldReturnNotFoundWithId()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostTag?)null);

        var command = new DeleteTagCommand(nonExistentId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Message.ShouldContain(nonExistentId.ToString());
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var existingTag = CreateTestTag(id: tagId);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        _postRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PostsWithTagForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteTagCommand(tagId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _tagRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<TagByIdForUpdateSpec>(), token),
            Times.Once);
        _postRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PostsWithTagForUpdateSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPostHasMultipleTags_ShouldOnlyRemoveSpecificTag()
    {
        // Arrange
        var tagIdToDelete = Guid.NewGuid();
        var otherTagId = Guid.NewGuid();
        var tagToDelete = CreateTestTag(id: tagIdToDelete, name: "Tag to Delete");

        var postId = Guid.NewGuid();
        var post = CreateTestPost(id: postId);

        // Add multiple tag assignments
        var assignmentToRemove = PostTagAssignment.Create(postId, tagIdToDelete, TestTenantId);
        var otherAssignment = PostTagAssignment.Create(postId, otherTagId, TestTenantId);

        post.TagAssignments.Add(assignmentToRemove);
        post.TagAssignments.Add(otherAssignment);

        var postsWithTag = new List<Post> { post };

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tagToDelete);

        _postRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PostsWithTagForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(postsWithTag);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteTagCommand(tagIdToDelete);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);

        // Should only remove the specific tag assignment
        post.TagAssignments.Count().ShouldBe(1);
        post.TagAssignments.First().TagId.ShouldBe(otherTagId);
    }

    [Fact]
    public async Task Handle_VerifySoftDeleteBehavior()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var existingTag = CreateTestTag(id: tagId);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        _postRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PostsWithTagForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteTagCommand(tagId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);

        // Verify Remove was called (soft delete is handled by EF interceptor)
        _tagRepositoryMock.Verify(
            x => x.Remove(existingTag),
            Times.Once);
    }

    #endregion
}
