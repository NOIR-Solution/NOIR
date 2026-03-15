using NOIR.Application.Features.Pm.Commands.DeleteTaskComment;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class DeleteTaskCommentCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly DeleteTaskCommentCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public DeleteTaskCommentCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new DeleteTaskCommentCommandHandler(
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldDeleteComment()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        var comment = TaskComment.Create(Guid.NewGuid(), Guid.NewGuid(), "Test comment", TestTenantId);
        typeof(TaskComment).GetProperty("Id")!.SetValue(comment, commentId);

        var comments = new List<TaskComment> { comment }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskComments).Returns(comments.Object);

        var command = new DeleteTaskCommentCommand(Guid.NewGuid(), commentId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Content.ShouldBe("Test comment");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CommentNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var emptyComments = new List<TaskComment>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskComments).Returns(emptyComments.Object);

        var command = new DeleteTaskCommentCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnDtoBeforeDelete()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var comment = TaskComment.Create(Guid.NewGuid(), authorId, "Important note", TestTenantId);
        typeof(TaskComment).GetProperty("Id")!.SetValue(comment, commentId);

        var comments = new List<TaskComment> { comment }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskComments).Returns(comments.Object);

        var command = new DeleteTaskCommentCommand(Guid.NewGuid(), commentId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Id.ShouldBe(commentId);
        result.Value.AuthorId.ShouldBe(authorId);
    }
}
