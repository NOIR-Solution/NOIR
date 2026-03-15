using NOIR.Application.Features.Pm.Commands.RemoveLabelFromTask;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class RemoveLabelFromTaskCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RemoveLabelFromTaskCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public RemoveLabelFromTaskCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new RemoveLabelFromTaskCommandHandler(
            _dbContextMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldRemoveLabelFromTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var labelId = Guid.NewGuid();
        var junction = ProjectTaskLabel.Create(taskId, labelId, TestTenantId);

        var taskLabels = new List<ProjectTaskLabel> { junction }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectTaskLabels).Returns(taskLabels.Object);

        var label = TaskLabel.Create(Guid.NewGuid(), "Bug", "#EF4444", TestTenantId);
        typeof(TaskLabel).GetProperty("Id")!.SetValue(label, labelId);
        var labels = new List<TaskLabel> { label }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskLabels).Returns(labels.Object);

        var command = new RemoveLabelFromTaskCommand(taskId, labelId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("Bug");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_JunctionNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var emptyTaskLabels = new List<ProjectTaskLabel>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectTaskLabels).Returns(emptyTaskLabels.Object);

        var command = new RemoveLabelFromTaskCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LabelNotFound_ShouldReturnFallbackDto()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var labelId = Guid.NewGuid();
        var junction = ProjectTaskLabel.Create(taskId, labelId, TestTenantId);

        var taskLabels = new List<ProjectTaskLabel> { junction }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectTaskLabels).Returns(taskLabels.Object);

        var emptyLabels = new List<TaskLabel>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskLabels).Returns(emptyLabels.Object);

        var command = new RemoveLabelFromTaskCommand(taskId, labelId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Id.ShouldBe(labelId);
        result.Value.Name.ShouldBeEmpty();
    }
}
