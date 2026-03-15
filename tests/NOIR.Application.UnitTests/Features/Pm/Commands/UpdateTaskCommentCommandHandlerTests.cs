using NOIR.Application.Features.Pm.Commands.UpdateTaskComment;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class UpdateTaskCommentCommandHandlerTests
{
    private readonly Mock<IRepository<Employee, Guid>> _employeeRepoMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly UpdateTaskCommentCommandHandler _handler;

    private const string TestTenantId = "tenant-123";
    private const string TestUserId = "user-123";

    public UpdateTaskCommentCommandHandlerTests()
    {
        _employeeRepoMock = new Mock<IRepository<Employee, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _currentUserMock.Setup(x => x.UserId).Returns(TestUserId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new UpdateTaskCommentCommandHandler(
            _employeeRepoMock.Object,
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static Employee CreateTestEmployee() =>
        Employee.Create("EMP-001", "John", "Doe", "john@test.com",
            Guid.NewGuid(), DateTimeOffset.UtcNow, EmploymentType.FullTime, TestTenantId);

    [Fact]
    public async Task Handle_ValidRequest_ShouldUpdateComment()
    {
        // Arrange
        var employee = CreateTestEmployee();
        var comment = TaskComment.Create(Guid.NewGuid(), employee.Id, "Original content", TestTenantId);
        var comments = new List<TaskComment> { comment }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskComments).Returns(comments.Object);

        _employeeRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var command = new UpdateTaskCommentCommand(comment.TaskId, comment.Id, "Updated content");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Content.ShouldBe("Updated content");
        result.Value.IsEdited.ShouldBe(true);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CommentNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var emptyComments = new List<TaskComment>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskComments).Returns(emptyComments.Object);

        var command = new UpdateTaskCommentCommand(Guid.NewGuid(), Guid.NewGuid(), "Updated content");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotAuthor_ShouldReturnForbidden()
    {
        // Arrange
        var originalAuthorId = Guid.NewGuid();
        var comment = TaskComment.Create(Guid.NewGuid(), originalAuthorId, "Original content", TestTenantId);
        var comments = new List<TaskComment> { comment }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskComments).Returns(comments.Object);

        // Current user is a different employee
        var differentEmployee = CreateTestEmployee();
        _employeeRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(differentEmployee);

        var command = new UpdateTaskCommentCommand(comment.TaskId, comment.Id, "Updated content");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
