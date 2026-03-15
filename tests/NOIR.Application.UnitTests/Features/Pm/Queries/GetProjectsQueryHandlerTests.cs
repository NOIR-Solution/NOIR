using NOIR.Application.Features.Pm.Queries.GetProjects;
using NOIR.Application.Features.Pm.Specifications;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Queries;

public class GetProjectsQueryHandlerTests
{
    private readonly Mock<IRepository<Project, Guid>> _projectRepoMock;
    private readonly GetProjectsQueryHandler _handler;

    private const string TestTenantId = "tenant-123";

    public GetProjectsQueryHandlerTests()
    {
        _projectRepoMock = new Mock<IRepository<Project, Guid>>();
        _handler = new GetProjectsQueryHandler(_projectRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnPagedResult()
    {
        // Arrange
        var projects = new List<Project>
        {
            Project.Create("Project 1", "project-1", "PRJ-001", TestTenantId),
            Project.Create("Project 2", "project-2", "PRJ-002", TestTenantId)
        };

        _projectRepoMock
            .Setup(x => x.ListAsync(It.IsAny<ProjectsByFilterSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        _projectRepoMock
            .Setup(x => x.CountAsync(It.IsAny<ProjectsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var query = new GetProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(2);
        result.Value.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_EmptyResult_ShouldReturnEmptyPagedResult()
    {
        // Arrange
        _projectRepoMock
            .Setup(x => x.ListAsync(It.IsAny<ProjectsByFilterSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());

        _projectRepoMock
            .Setup(x => x.CountAsync(It.IsAny<ProjectsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetProjectsQuery(Search: "nonexistent");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldCallRepoWithSpec()
    {
        // Arrange
        _projectRepoMock
            .Setup(x => x.ListAsync(It.IsAny<ProjectsByFilterSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());

        _projectRepoMock
            .Setup(x => x.CountAsync(It.IsAny<ProjectsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetProjectsQuery(Status: ProjectStatus.Active);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _projectRepoMock.Verify(
            x => x.ListAsync(It.IsAny<ProjectsByFilterSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _projectRepoMock.Verify(
            x => x.CountAsync(It.IsAny<ProjectsCountSpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
