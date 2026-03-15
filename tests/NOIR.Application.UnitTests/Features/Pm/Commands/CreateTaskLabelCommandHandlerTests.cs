using NOIR.Application.Features.Pm.Commands.CreateTaskLabel;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class CreateTaskLabelCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly CreateTaskLabelCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public CreateTaskLabelCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new CreateTaskLabelCommandHandler(
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCreateLabel()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var emptyLabels = new List<TaskLabel>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskLabels).Returns(emptyLabels.Object);

        var command = new CreateTaskLabelCommand(projectId, "Bug", "#EF4444");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("Bug");
        result.Value.Color.ShouldBe("#EF4444");
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateNameInProject_ShouldReturnError()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var existingLabel = TaskLabel.Create(projectId, "Bug", "#EF4444", TestTenantId);
        var labels = new List<TaskLabel> { existingLabel }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskLabels).Returns(labels.Object);

        var command = new CreateTaskLabelCommand(projectId, "Bug", "#3B82F6");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
