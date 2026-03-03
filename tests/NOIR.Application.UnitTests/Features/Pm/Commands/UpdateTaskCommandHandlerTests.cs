using NOIR.Application.Features.Pm.Commands.UpdateTask;
using NOIR.Application.Features.Pm.Specifications;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class UpdateTaskCommandHandlerTests
{
    private readonly Mock<IRepository<ProjectTask, Guid>> _taskRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly UpdateTaskCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public UpdateTaskCommandHandlerTests()
    {
        _taskRepoMock = new Mock<IRepository<ProjectTask, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new UpdateTaskCommandHandler(
            _taskRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldUpdateTask()
    {
        // Arrange
        var task = ProjectTask.Create(Guid.NewGuid(), "PRJ-001", "Original Title", TestTenantId);

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var command = new UpdateTaskCommand(task.Id, Title: "Updated Title", Priority: TaskPriority.High);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        task.Title.Should().Be("Updated Title");
        task.Priority.Should().Be(TaskPriority.High);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TaskNotFound_ShouldReturnError()
    {
        // Arrange
        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask?)null);

        var command = new UpdateTaskCommand(Guid.NewGuid(), Title: "New Title");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PartialUpdate_ShouldPreserveExistingValues()
    {
        // Arrange
        var task = ProjectTask.Create(Guid.NewGuid(), "PRJ-001", "Original Title", TestTenantId,
            description: "Original description", priority: TaskPriority.Medium);

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Only update title, not description or priority
        var command = new UpdateTaskCommand(task.Id, Title: "Updated Title");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        task.Title.Should().Be("Updated Title");
        task.Description.Should().Be("Original description");
    }

    [Fact]
    public async Task Handle_FullUpdate_ShouldUpdateAllFields()
    {
        // Arrange
        var assigneeId = Guid.NewGuid();
        var task = ProjectTask.Create(Guid.NewGuid(), "PRJ-001", "Original", TestTenantId);

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var dueDate = DateTimeOffset.UtcNow.AddDays(7);
        var command = new UpdateTaskCommand(
            task.Id,
            Title: "Full Updated Title",
            Description: "Full description",
            Priority: TaskPriority.Urgent,
            AssigneeId: assigneeId,
            DueDate: dueDate,
            EstimatedHours: 16m,
            ActualHours: 4m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        task.Title.Should().Be("Full Updated Title");
        task.Description.Should().Be("Full description");
        task.Priority.Should().Be(TaskPriority.Urgent);
        task.AssigneeId.Should().Be(assigneeId);
        task.DueDate.Should().Be(dueDate);
        task.EstimatedHours.Should().Be(16m);
        task.ActualHours.Should().Be(4m);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
