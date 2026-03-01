using NOIR.Application.Features.Pm.Commands.ChangeTaskStatus;
using NOIR.Application.Features.Pm.Specifications;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class ChangeTaskStatusCommandHandlerTests
{
    private readonly Mock<IRepository<ProjectTask, Guid>> _taskRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ChangeTaskStatusCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public ChangeTaskStatusCommandHandlerTests()
    {
        _taskRepoMock = new Mock<IRepository<ProjectTask, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new ChangeTaskStatusCommandHandler(
            _taskRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldChangeStatusToInProgress()
    {
        // Arrange
        var task = ProjectTask.Create(Guid.NewGuid(), "PRJ-1", "Test Task", TestTenantId,
            columnId: Guid.NewGuid());

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var command = new ChangeTaskStatusCommand(task.Id, ProjectTaskStatus.InProgress);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(ProjectTaskStatus.InProgress);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_StatusDone_ShouldCallComplete()
    {
        // Arrange
        var task = ProjectTask.Create(Guid.NewGuid(), "PRJ-1", "Test Task", TestTenantId,
            columnId: Guid.NewGuid());

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var command = new ChangeTaskStatusCommand(task.Id, ProjectTaskStatus.Done);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(ProjectTaskStatus.Done);
        task.CompletedAt.Should().NotBeNull();
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

        var command = new ChangeTaskStatusCommand(Guid.NewGuid(), ProjectTaskStatus.InProgress);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
