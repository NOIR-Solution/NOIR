using NOIR.Application.Features.Pm.Queries.GetTaskById;
using NOIR.Application.Features.Pm.Specifications;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Queries;

public class GetTaskByIdQueryHandlerTests
{
    private readonly Mock<IRepository<ProjectTask, Guid>> _taskRepoMock;
    private readonly GetTaskByIdQueryHandler _handler;

    private const string TestTenantId = "tenant-123";

    public GetTaskByIdQueryHandlerTests()
    {
        _taskRepoMock = new Mock<IRepository<ProjectTask, Guid>>();
        _handler = new GetTaskByIdQueryHandler(_taskRepoMock.Object);
    }

    [Fact]
    public async Task Handle_TaskExists_ShouldReturnTaskDto()
    {
        // Arrange
        var task = ProjectTask.Create(Guid.NewGuid(), "PRJ-001", "Test Task", TestTenantId);

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var query = new GetTaskByIdQuery(task.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Title.ShouldBe("Test Task");
        result.Value.TaskNumber.ShouldBe("PRJ-001");
    }

    [Fact]
    public async Task Handle_TaskNotFound_ShouldReturnError()
    {
        // Arrange
        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask?)null);

        var query = new GetTaskByIdQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_ShouldUseTaskByIdSpec()
    {
        // Arrange
        var task = ProjectTask.Create(Guid.NewGuid(), "PRJ-001", "Task", TestTenantId);

        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var query = new GetTaskByIdQuery(Guid.NewGuid());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _taskRepoMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
