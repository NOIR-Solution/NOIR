using NOIR.Application.Features.Pm.Commands.CreateColumn;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class CreateColumnCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly CreateColumnCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public CreateColumnCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new CreateColumnCommandHandler(
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCreateColumn()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var emptyColumns = new List<ProjectColumn>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(emptyColumns.Object);

        var command = new CreateColumnCommand(projectId, "In Progress", "#3B82F6", 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("In Progress");
        result.Value.SortOrder.Should().Be(0);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingColumns_ShouldSetCorrectSortOrder()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var existingColumn = ProjectColumn.Create(projectId, "Todo", 0, TestTenantId);
        var columns = new List<ProjectColumn> { existingColumn }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(columns.Object);

        var command = new CreateColumnCommand(projectId, "In Progress");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SortOrder.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnDtoWithCorrectProperties()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var emptyColumns = new List<ProjectColumn>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(emptyColumns.Object);

        var command = new CreateColumnCommand(projectId, "Done", "#22C55E", 10);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Done");
        result.Value.Color.Should().Be("#22C55E");
        result.Value.WipLimit.Should().Be(10);
    }
}
