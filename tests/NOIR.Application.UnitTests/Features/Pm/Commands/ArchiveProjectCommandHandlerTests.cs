using NOIR.Application.Features.Pm.Commands.ArchiveProject;
using NOIR.Application.Features.Pm.Specifications;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class ArchiveProjectCommandHandlerTests
{
    private readonly Mock<IRepository<Project, Guid>> _projectRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly ArchiveProjectCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public ArchiveProjectCommandHandlerTests()
    {
        _projectRepoMock = new Mock<IRepository<Project, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new ArchiveProjectCommandHandler(
            _projectRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldArchiveProject()
    {
        // Arrange
        var project = Project.Create("Test Project", "test-project", "PRJ-20260301-000001", TestTenantId);

        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new ArchiveProjectCommand(project.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        project.Status.Should().Be(ProjectStatus.Archived);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ShouldReturnError()
    {
        // Arrange
        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var command = new ArchiveProjectCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
