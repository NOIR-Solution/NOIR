using NOIR.Application.Features.Pm.Commands.ReorderTask;
using NOIR.Application.Features.Pm.Specifications;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class ReorderTaskCommandHandlerTests
{
    private readonly Mock<IRepository<ProjectTask, Guid>> _taskRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ReorderTaskCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public ReorderTaskCommandHandlerTests()
    {
        _taskRepoMock = new Mock<IRepository<ProjectTask, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new ReorderTaskCommandHandler(
            _taskRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldUpdateSortOrder()
    {
        // Arrange
        var columnId = Guid.NewGuid();
        var task = ProjectTask.Create(Guid.NewGuid(), "PRJ-001", "Test Task", TestTenantId,
            columnId: columnId);

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var command = new ReorderTaskCommand(task.Id, 5.5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        task.SortOrder.ShouldBe(5.5);
        task.ColumnId.ShouldBe(columnId); // Column should remain the same
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TaskNotFound_ShouldReturnError()
    {
        // Arrange
        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask?)null);

        var command = new ReorderTaskCommand(Guid.NewGuid(), 1.0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldPreserveColumnId()
    {
        // Arrange
        var columnId = Guid.NewGuid();
        var task = ProjectTask.Create(Guid.NewGuid(), "PRJ-002", "Another Task", TestTenantId,
            columnId: columnId);

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var command = new ReorderTaskCommand(task.Id, 10.0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        task.ColumnId.ShouldBe(columnId);
    }

    [Fact]
    public async Task Handle_TaskWithoutColumn_ShouldReturnValidationError()
    {
        // Arrange — task created without a column (ColumnId is null)
        var task = ProjectTask.Create(Guid.NewGuid(), "PRJ-003", "Unassigned Task", TestTenantId);

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var command = new ReorderTaskCommand(task.Id, 3.0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
