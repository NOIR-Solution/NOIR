using NOIR.Application.Features.Pm.Commands.ChangeProjectStatus;
using NOIR.Application.Features.Pm.Specifications;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class ChangeProjectStatusCommandHandlerTests
{
    private readonly Mock<IRepository<Project, Guid>> _projectRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly ChangeProjectStatusCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public ChangeProjectStatusCommandHandlerTests()
    {
        _projectRepoMock = new Mock<IRepository<Project, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new ChangeProjectStatusCommandHandler(
            _projectRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidTransition_ShouldChangeStatus()
    {
        // Arrange
        var project = Project.Create("Test Project", "test-project", "PRJ-20260301-000001", TestTenantId);

        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new ChangeProjectStatusCommand(project.Id, ProjectStatus.OnHold);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ShouldReturnError()
    {
        // Arrange
        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var command = new ChangeProjectStatusCommand(Guid.NewGuid(), ProjectStatus.OnHold);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SameStatus_ShouldReturnValidationError()
    {
        // Arrange
        var project = Project.Create("Test Project", "test-project", "PRJ-20260301-000001", TestTenantId);

        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Active -> Active (same status)
        var command = new ChangeProjectStatusCommand(project.Id, ProjectStatus.Active);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidTransitionFromArchived_ShouldReturnError()
    {
        // Arrange
        var project = Project.Create("Test Project", "test-project", "PRJ-20260301-000001", TestTenantId);
        project.ChangeStatus(ProjectStatus.Archived);

        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Archived -> Active (not allowed)
        var command = new ChangeProjectStatusCommand(project.Id, ProjectStatus.Active);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OnHoldToActive_ShouldSucceed()
    {
        // Arrange
        var project = Project.Create("Test Project", "test-project", "PRJ-20260301-000001", TestTenantId);
        project.ChangeStatus(ProjectStatus.OnHold);

        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new ChangeProjectStatusCommand(project.Id, ProjectStatus.Active);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ActiveToCompleted_ShouldSucceed()
    {
        // Arrange
        var project = Project.Create("Test Project", "test-project", "PRJ-20260301-000001", TestTenantId);

        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new ChangeProjectStatusCommand(project.Id, ProjectStatus.Completed);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
