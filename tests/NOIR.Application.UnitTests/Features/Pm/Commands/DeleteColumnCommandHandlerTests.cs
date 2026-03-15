using NOIR.Application.Features.Pm.Commands.DeleteColumn;
using NOIR.Application.Features.Pm.Specifications;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class DeleteColumnCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IRepository<ProjectTask, Guid>> _taskRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly DeleteColumnCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public DeleteColumnCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _taskRepoMock = new Mock<IRepository<ProjectTask, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new DeleteColumnCommandHandler(
            _dbContextMock.Object,
            _taskRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldDeleteColumn()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var columnId = Guid.NewGuid();
        var targetColumnId = Guid.NewGuid();

        var column = ProjectColumn.Create(projectId, "To Delete", 0, TestTenantId);
        typeof(ProjectColumn).GetProperty("Id")!.SetValue(column, columnId);

        var targetColumn = ProjectColumn.Create(projectId, "Target", 1, TestTenantId);
        typeof(ProjectColumn).GetProperty("Id")!.SetValue(targetColumn, targetColumnId);

        var columns = new List<ProjectColumn> { column, targetColumn }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(columns.Object);

        _taskRepoMock
            .Setup(x => x.ListAsync(It.IsAny<TasksByColumnSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectTask>());

        var command = new DeleteColumnCommand(projectId, columnId, targetColumnId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ColumnNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var emptyColumns = new List<ProjectColumn>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(emptyColumns.Object);

        var command = new DeleteColumnCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SameColumnAsMoveTarget_ShouldReturnValidationError()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var columnId = Guid.NewGuid();

        var column = ProjectColumn.Create(projectId, "Column", 0, TestTenantId);
        typeof(ProjectColumn).GetProperty("Id")!.SetValue(column, columnId);

        var columns = new List<ProjectColumn> { column }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(columns.Object);

        // Same columnId for both source and target
        var command = new DeleteColumnCommand(projectId, columnId, columnId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TargetColumnNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var columnId = Guid.NewGuid();
        var targetColumnId = Guid.NewGuid();

        var column = ProjectColumn.Create(projectId, "Source Column", 0, TestTenantId);
        typeof(ProjectColumn).GetProperty("Id")!.SetValue(column, columnId);

        // Only source column exists, target does not
        var columns = new List<ProjectColumn> { column }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(columns.Object);

        var command = new DeleteColumnCommand(projectId, columnId, targetColumnId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithTasks_ShouldMigrateTasksToTargetColumn()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var columnId = Guid.NewGuid();
        var targetColumnId = Guid.NewGuid();

        var column = ProjectColumn.Create(projectId, "To Delete", 0, TestTenantId);
        typeof(ProjectColumn).GetProperty("Id")!.SetValue(column, columnId);

        var targetColumn = ProjectColumn.Create(projectId, "Target", 1, TestTenantId);
        typeof(ProjectColumn).GetProperty("Id")!.SetValue(targetColumn, targetColumnId);

        var columns = new List<ProjectColumn> { column, targetColumn }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(columns.Object);

        // Tasks in the column being deleted
        var task1 = ProjectTask.Create(projectId, "PRJ-001", "Task 1", TestTenantId, columnId: columnId);
        var task2 = ProjectTask.Create(projectId, "PRJ-002", "Task 2", TestTenantId, columnId: columnId);

        _taskRepoMock
            .Setup(x => x.ListAsync(It.IsAny<TasksByColumnSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectTask> { task1, task2 });

        // Return tracked versions for update
        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskByIdForUpdateSpec spec, CancellationToken _) =>
            {
                // Return the matching task
                if (spec.ToString()!.Contains(task1.Id.ToString())) return task1;
                if (spec.ToString()!.Contains(task2.Id.ToString())) return task2;
                return task1; // fallback
            });

        var command = new DeleteColumnCommand(projectId, columnId, targetColumnId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _taskRepoMock.Verify(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdForUpdateSpec>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
