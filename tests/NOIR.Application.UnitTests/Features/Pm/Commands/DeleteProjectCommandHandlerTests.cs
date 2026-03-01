using NOIR.Application.Features.Pm.Commands.DeleteProject;
using NOIR.Application.Features.Pm.Specifications;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class DeleteProjectCommandHandlerTests
{
    private readonly Mock<IRepository<Project, Guid>> _projectRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly DeleteProjectCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public DeleteProjectCommandHandlerTests()
    {
        _projectRepoMock = new Mock<IRepository<Project, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new DeleteProjectCommandHandler(
            _projectRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldDeleteProject()
    {
        // Arrange
        var project = Project.Create("Test Project", "test-project", "PRJ-20260301-000001", TestTenantId);

        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new DeleteProjectCommand(project.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _projectRepoMock.Verify(
            x => x.Remove(It.IsAny<Project>()), Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ShouldReturnError()
    {
        // Arrange
        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var command = new DeleteProjectCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _projectRepoMock.Verify(
            x => x.Remove(It.IsAny<Project>()), Times.Never);
    }
}
