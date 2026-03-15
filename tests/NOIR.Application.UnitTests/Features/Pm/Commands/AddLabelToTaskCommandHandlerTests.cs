using NOIR.Application.Features.Pm.Commands.AddLabelToTask;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class AddLabelToTaskCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly AddLabelToTaskCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public AddLabelToTaskCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new AddLabelToTaskCommandHandler(
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldAddLabelToTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var labelId = Guid.NewGuid();
        var label = TaskLabel.Create(Guid.NewGuid(), "Bug", "#EF4444", TestTenantId);
        typeof(TaskLabel).GetProperty("Id")!.SetValue(label, labelId);

        var emptyTaskLabels = new List<ProjectTaskLabel>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectTaskLabels).Returns(emptyTaskLabels.Object);

        var labels = new List<TaskLabel> { label }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskLabels).Returns(labels.Object);

        var command = new AddLabelToTaskCommand(taskId, labelId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("Bug");
        result.Value.Color.ShouldBe("#EF4444");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LabelAlreadyAssigned_ShouldReturnConflict()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var labelId = Guid.NewGuid();
        var existing = ProjectTaskLabel.Create(taskId, labelId, TestTenantId);

        var taskLabels = new List<ProjectTaskLabel> { existing }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectTaskLabels).Returns(taskLabels.Object);

        var command = new AddLabelToTaskCommand(taskId, labelId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LabelNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var labelId = Guid.NewGuid();

        var emptyTaskLabels = new List<ProjectTaskLabel>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectTaskLabels).Returns(emptyTaskLabels.Object);

        var emptyLabels = new List<TaskLabel>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskLabels).Returns(emptyLabels.Object);

        var command = new AddLabelToTaskCommand(taskId, labelId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
