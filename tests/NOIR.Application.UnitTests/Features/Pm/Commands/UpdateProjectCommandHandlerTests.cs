using NOIR.Application.Features.Pm.Commands.UpdateProject;
using NOIR.Application.Features.Pm.Specifications;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class UpdateProjectCommandHandlerTests
{
    private readonly Mock<IRepository<Project, Guid>> _projectRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdateProjectCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public UpdateProjectCommandHandlerTests()
    {
        _projectRepoMock = new Mock<IRepository<Project, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new UpdateProjectCommandHandler(
            _projectRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldUpdateProject()
    {
        // Arrange
        var project = Project.Create("Old Name", "old-name", "PRJ-20260301-000001", TestTenantId);

        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectBySlugSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new UpdateProjectCommand(project.Id, "Updated Name", Description: "New desc");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
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

        var command = new UpdateProjectCommand(Guid.NewGuid(), "Name");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
