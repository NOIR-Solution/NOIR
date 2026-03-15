using NOIR.Application.Features.Pm.Queries.GetKanbanBoard;
using NOIR.Application.Features.Pm.Specifications;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Queries;

public class GetKanbanBoardQueryHandlerTests
{
    private readonly Mock<IRepository<Project, Guid>> _projectRepoMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IRepository<ProjectTask, Guid>> _taskRepoMock;
    private readonly GetKanbanBoardQueryHandler _handler;

    private const string TestTenantId = "tenant-123";

    public GetKanbanBoardQueryHandlerTests()
    {
        _projectRepoMock = new Mock<IRepository<Project, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _taskRepoMock = new Mock<IRepository<ProjectTask, Guid>>();

        _handler = new GetKanbanBoardQueryHandler(
            _projectRepoMock.Object,
            _dbContextMock.Object,
            _taskRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ProjectExists_ShouldReturnKanbanBoard()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = Project.Create("Test Project", "test-project", "PRJ-001", TestTenantId);
        typeof(Project).GetProperty("Id")!.SetValue(project, projectId);

        _projectRepoMock
            .Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var columns = new List<ProjectColumn>
        {
            ProjectColumn.Create(projectId, "Todo", 0, TestTenantId),
            ProjectColumn.Create(projectId, "Done", 1, TestTenantId)
        };
        var columnDbSet = columns.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(columnDbSet.Object);

        _taskRepoMock
            .Setup(x => x.ListAsync(It.IsAny<TasksForKanbanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectTask>());

        var query = new GetKanbanBoardQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ProjectName.ShouldBe("Test Project");
        result.Value.Columns.Count().ShouldBe(2);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ShouldReturnError()
    {
        // Arrange
        _projectRepoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var query = new GetKanbanBoardQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_EmptyColumns_ShouldReturnEmptyBoard()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = Project.Create("Test", "test", "PRJ-001", TestTenantId);
        typeof(Project).GetProperty("Id")!.SetValue(project, projectId);

        _projectRepoMock
            .Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var emptyColumns = new List<ProjectColumn>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(emptyColumns.Object);

        _taskRepoMock
            .Setup(x => x.ListAsync(It.IsAny<TasksForKanbanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectTask>());

        var query = new GetKanbanBoardQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Columns.ShouldBeEmpty();
    }
}
