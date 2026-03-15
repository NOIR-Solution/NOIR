using NOIR.Application.Features.Pm.Commands.UpdateColumn;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class UpdateColumnCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly UpdateColumnCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public UpdateColumnCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new UpdateColumnCommandHandler(
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldUpdateColumn()
    {
        // Arrange
        var columnId = Guid.NewGuid();
        var column = ProjectColumn.Create(Guid.NewGuid(), "Todo", 0, TestTenantId);
        typeof(ProjectColumn).GetProperty("Id")!.SetValue(column, columnId);

        var columns = new List<ProjectColumn> { column }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(columns.Object);

        var command = new UpdateColumnCommand(Guid.NewGuid(), columnId, "In Progress", "#3B82F6", 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("In Progress");
        result.Value.Color.ShouldBe("#3B82F6");
        result.Value.WipLimit.ShouldBe(5);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ColumnNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var emptyColumns = new List<ProjectColumn>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(emptyColumns.Object);

        var command = new UpdateColumnCommand(Guid.NewGuid(), Guid.NewGuid(), "Updated");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldPreserveSortOrder()
    {
        // Arrange
        var columnId = Guid.NewGuid();
        var column = ProjectColumn.Create(Guid.NewGuid(), "Todo", 3, TestTenantId);
        typeof(ProjectColumn).GetProperty("Id")!.SetValue(column, columnId);

        var columns = new List<ProjectColumn> { column }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(columns.Object);

        var command = new UpdateColumnCommand(Guid.NewGuid(), columnId, "Updated Name");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.SortOrder.ShouldBe(3);
    }
}
