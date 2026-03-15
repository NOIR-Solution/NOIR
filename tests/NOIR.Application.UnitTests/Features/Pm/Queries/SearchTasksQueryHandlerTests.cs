using NOIR.Application.Features.Pm.Queries.SearchTasks;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Queries;

public class SearchTasksQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly SearchTasksQueryHandler _handler;

    private const string TestTenantId = "tenant-123";

    public SearchTasksQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _handler = new SearchTasksQueryHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_EmptySearchText_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new SearchTasksQuery(Guid.NewGuid(), "");

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
        var query = new SearchTasksQuery(Guid.NewGuid(), "   ");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ValidSearchText_ShouldQueryTasks()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var tasks = new List<ProjectTask>
        {
            ProjectTask.Create(projectId, "PRJ-001", "Setup CI/CD", TestTenantId)
        };
        var tasksDbSet = tasks.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectTasks).Returns(tasksDbSet.Object);

        var query = new SearchTasksQuery(projectId, "Setup");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_NoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        var emptyTasks = new List<ProjectTask>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectTasks).Returns(emptyTasks.Object);

        var query = new SearchTasksQuery(Guid.NewGuid(), "nonexistent");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBeEmpty();
    }
}
