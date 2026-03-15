using NOIR.Application.Features.Pm.Queries.GetTasks;
using NOIR.Application.Features.Pm.Specifications;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Queries;

public class GetTasksQueryHandlerTests
{
    private readonly Mock<IRepository<ProjectTask, Guid>> _taskRepoMock;
    private readonly GetTasksQueryHandler _handler;

    private const string TestTenantId = "tenant-123";

    public GetTasksQueryHandlerTests()
    {
        _taskRepoMock = new Mock<IRepository<ProjectTask, Guid>>();
        _handler = new GetTasksQueryHandler(_taskRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnPagedResult()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var tasks = new List<ProjectTask>
        {
            ProjectTask.Create(projectId, "PRJ-001", "Task 1", TestTenantId),
            ProjectTask.Create(projectId, "PRJ-002", "Task 2", TestTenantId)
        };

        _taskRepoMock
            .Setup(x => x.ListAsync(It.IsAny<TasksByProjectSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        _taskRepoMock
            .Setup(x => x.CountAsync(It.IsAny<TasksByProjectCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var query = new GetTasksQuery(projectId);

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
        _taskRepoMock
            .Setup(x => x.ListAsync(It.IsAny<TasksByProjectSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectTask>());

        _taskRepoMock
            .Setup(x => x.CountAsync(It.IsAny<TasksByProjectCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetTasksQuery(Guid.NewGuid(), Search: "nonexistent");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WithFilters_ShouldCallRepoWithSpec()
    {
        // Arrange
        _taskRepoMock
            .Setup(x => x.ListAsync(It.IsAny<TasksByProjectSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectTask>());

        _taskRepoMock
            .Setup(x => x.CountAsync(It.IsAny<TasksByProjectCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetTasksQuery(Guid.NewGuid(), Status: ProjectTaskStatus.InProgress, Priority: TaskPriority.High);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _taskRepoMock.Verify(
            x => x.ListAsync(It.IsAny<TasksByProjectSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _taskRepoMock.Verify(
            x => x.CountAsync(It.IsAny<TasksByProjectCountSpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
