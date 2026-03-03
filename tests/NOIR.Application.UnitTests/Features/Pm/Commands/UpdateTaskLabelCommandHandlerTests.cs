using NOIR.Application.Features.Pm.Commands.UpdateTaskLabel;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class UpdateTaskLabelCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly UpdateTaskLabelCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public UpdateTaskLabelCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new UpdateTaskLabelCommandHandler(
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldUpdateLabel()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var label = TaskLabel.Create(projectId, "Bug", "#EF4444", TestTenantId);
        var labels = new List<TaskLabel> { label }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskLabels).Returns(labels.Object);

        var command = new UpdateTaskLabelCommand(projectId, label.Id, "Feature", "#3B82F6");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Feature");
        result.Value.Color.Should().Be("#3B82F6");
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LabelNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var emptyLabels = new List<TaskLabel>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskLabels).Returns(emptyLabels.Object);

        var command = new UpdateTaskLabelCommand(Guid.NewGuid(), Guid.NewGuid(), "Feature", "#3B82F6");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateNameInProject_ShouldReturnConflict()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var existingLabel = TaskLabel.Create(projectId, "Bug", "#EF4444", TestTenantId);
        var labelToUpdate = TaskLabel.Create(projectId, "Feature", "#3B82F6", TestTenantId);
        var labels = new List<TaskLabel> { existingLabel, labelToUpdate }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskLabels).Returns(labels.Object);

        // Try to rename "Feature" to "Bug" which already exists
        var command = new UpdateTaskLabelCommand(projectId, labelToUpdate.Id, "Bug", "#3B82F6");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
