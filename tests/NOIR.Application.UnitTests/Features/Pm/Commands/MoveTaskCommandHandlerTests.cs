using NOIR.Application.Features.Pm.Commands.MoveTask;
using NOIR.Application.Features.Pm.Specifications;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class MoveTaskCommandHandlerTests
{
    private readonly Mock<IRepository<ProjectTask, Guid>> _taskRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly MoveTaskCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public MoveTaskCommandHandlerTests()
    {
        _taskRepoMock = new Mock<IRepository<ProjectTask, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new MoveTaskCommandHandler(
            _taskRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldMoveTaskToNewColumn()
    {
        // Arrange
        var task = ProjectTask.Create(Guid.NewGuid(), "PRJ-1", "Test Task", TestTenantId,
            columnId: Guid.NewGuid());
        var newColumnId = Guid.NewGuid();

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var command = new MoveTaskCommand(task.Id, newColumnId, 3.5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        task.ColumnId.ShouldBe(newColumnId);
        task.SortOrder.ShouldBe(3.5);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TaskNotFound_ShouldReturnError()
    {
        // Arrange
        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask?)null);

        var command = new MoveTaskCommand(Guid.NewGuid(), Guid.NewGuid(), 1.0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
