using NOIR.Application.Features.Pm.Queries.GetProjectLabels;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Queries;

public class GetProjectLabelsQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly GetProjectLabelsQueryHandler _handler;

    private const string TestTenantId = "tenant-123";

    public GetProjectLabelsQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _handler = new GetProjectLabelsQueryHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_LabelsExist_ShouldReturnLabelList()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var labels = new List<TaskLabel>
        {
            TaskLabel.Create(projectId, "Bug", "#EF4444", TestTenantId),
            TaskLabel.Create(projectId, "Feature", "#3B82F6", TestTenantId)
        };
        var labelsDbSet = labels.BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskLabels).Returns(labelsDbSet.Object);

        var query = new GetProjectLabelsQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Count().ShouldBe(2);
    }

    [Fact]
    public async Task Handle_NoLabels_ShouldReturnEmptyList()
    {
        // Arrange
        var emptyLabels = new List<TaskLabel>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskLabels).Returns(emptyLabels.Object);

        var query = new GetProjectLabelsQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectDtoProperties()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var label = TaskLabel.Create(projectId, "Urgent", "#F59E0B", TestTenantId);
        var labels = new List<TaskLabel> { label }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskLabels).Returns(labels.Object);

        var query = new GetProjectLabelsQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.First().Name.ShouldBe("Urgent");
        result.Value.First().Color.ShouldBe("#F59E0B");
    }
}
