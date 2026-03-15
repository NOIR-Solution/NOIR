using NOIR.Application.Features.Pm.Queries.GetProjectById;
using NOIR.Application.Features.Pm.Specifications;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Queries;

public class GetProjectByIdQueryHandlerTests
{
    private readonly Mock<IRepository<Project, Guid>> _projectRepoMock;
    private readonly GetProjectByIdQueryHandler _handler;

    private const string TestTenantId = "tenant-123";

    public GetProjectByIdQueryHandlerTests()
    {
        _projectRepoMock = new Mock<IRepository<Project, Guid>>();
        _handler = new GetProjectByIdQueryHandler(_projectRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ProjectExists_ShouldReturnProjectDto()
    {
        // Arrange
        var project = Project.Create("Test Project", "test-project", "PRJ-20260301-000001", TestTenantId);

        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var query = new GetProjectByIdQuery(project.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("Test Project");
        result.Value.Slug.ShouldBe("test-project");
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ShouldReturnError()
    {
        // Arrange
        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var query = new GetProjectByIdQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_ShouldUseProjectByIdSpec()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = Project.Create("Test", "test", "PRJ-20260301-000001", TestTenantId);

        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var query = new GetProjectByIdQuery(projectId);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _projectRepoMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
