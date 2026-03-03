using NOIR.Application.Features.Pm.Commands.AddSubtask;
using NOIR.Application.Features.Pm.Specifications;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class AddSubtaskCommandHandlerTests
{
    private readonly Mock<IRepository<ProjectTask, Guid>> _taskRepoMock;
    private readonly Mock<IRepository<Project, Guid>> _projectRepoMock;
    private readonly Mock<ITaskNumberGenerator> _taskNumberGeneratorMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly AddSubtaskCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public AddSubtaskCommandHandlerTests()
    {
        _taskRepoMock = new Mock<IRepository<ProjectTask, Guid>>();
        _projectRepoMock = new Mock<IRepository<Project, Guid>>();
        _taskNumberGeneratorMock = new Mock<ITaskNumberGenerator>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _taskNumberGeneratorMock
            .Setup(x => x.GenerateNextAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("PRJ-002");
        _taskRepoMock
            .Setup(x => x.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask t, CancellationToken _) => t);

        _handler = new AddSubtaskCommandHandler(
            _taskRepoMock.Object,
            _projectRepoMock.Object,
            _taskNumberGeneratorMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCreateSubtask()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var parentTask = ProjectTask.Create(projectId, "PRJ-001", "Parent Task", TestTenantId,
            columnId: Guid.NewGuid());
        var project = Project.Create("Test Project", "test-project", "PRJ-20260301-000001", TestTenantId);
        typeof(Project).GetProperty("Id")!.SetValue(project, projectId);

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentTask);

        _projectRepoMock
            .Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new AddSubtaskCommand(parentTask.Id, "Subtask Title", "Description", TaskPriority.Medium, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _taskRepoMock.Verify(x => x.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ParentTaskNotFound_ShouldReturnError()
    {
        // Arrange
        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask?)null);

        var command = new AddSubtaskCommand(Guid.NewGuid(), "Subtask", null, TaskPriority.Low, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _taskRepoMock.Verify(x => x.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ShouldReturnError()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var parentTask = ProjectTask.Create(projectId, "PRJ-001", "Parent Task", TestTenantId);

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentTask);

        _projectRepoMock
            .Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var command = new AddSubtaskCommand(parentTask.Id, "Subtask", null, TaskPriority.Low, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _taskRepoMock.Verify(x => x.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
