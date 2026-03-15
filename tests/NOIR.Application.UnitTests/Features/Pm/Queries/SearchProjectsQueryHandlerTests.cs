using NOIR.Application.Features.Pm.Queries.SearchProjects;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Queries;

public class SearchProjectsQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly SearchProjectsQueryHandler _handler;

    private const string TestTenantId = "tenant-123";

    public SearchProjectsQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _handler = new SearchProjectsQueryHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_EmptySearchText_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new SearchProjectsQuery("");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WhitespaceSearchText_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new SearchProjectsQuery("   ");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ValidSearchText_ShouldQueryProjects()
    {
        // Arrange
        var projects = new List<Project>
        {
            Project.Create("Alpha Project", "alpha-project", "PRJ-001", TestTenantId)
        };
        var projectsDbSet = projects.BuildMockDbSet();
        _dbContextMock.Setup(x => x.Projects).Returns(projectsDbSet.Object);

        var query = new SearchProjectsQuery("Alpha");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_NoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        var emptyProjects = new List<Project>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.Projects).Returns(emptyProjects.Object);

        var query = new SearchProjectsQuery("nonexistent");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBeEmpty();
    }
}
