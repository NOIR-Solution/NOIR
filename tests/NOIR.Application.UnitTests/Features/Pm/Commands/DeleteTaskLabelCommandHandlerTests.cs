using NOIR.Application.Features.Pm.Commands.DeleteTaskLabel;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class DeleteTaskLabelCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly DeleteTaskLabelCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public DeleteTaskLabelCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new DeleteTaskLabelCommandHandler(
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldDeleteLabel()
    {
        // Arrange
        var labelId = Guid.NewGuid();
        var label = TaskLabel.Create(Guid.NewGuid(), "Bug", "#EF4444", TestTenantId);
        typeof(TaskLabel).GetProperty("Id")!.SetValue(label, labelId);

        var labels = new List<TaskLabel> { label }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskLabels).Returns(labels.Object);

        var command = new DeleteTaskLabelCommand(Guid.NewGuid(), labelId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Bug");
        result.Value.Color.Should().Be("#EF4444");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LabelNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var emptyLabels = new List<TaskLabel>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskLabels).Returns(emptyLabels.Object);

        var command = new DeleteTaskLabelCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnDtoBeforeDelete()
    {
        // Arrange
        var labelId = Guid.NewGuid();
        var label = TaskLabel.Create(Guid.NewGuid(), "Feature", "#3B82F6", TestTenantId);
        typeof(TaskLabel).GetProperty("Id")!.SetValue(label, labelId);

        var labels = new List<TaskLabel> { label }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskLabels).Returns(labels.Object);

        var command = new DeleteTaskLabelCommand(Guid.NewGuid(), labelId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(labelId);
        result.Value.Name.Should().Be("Feature");
    }
}
