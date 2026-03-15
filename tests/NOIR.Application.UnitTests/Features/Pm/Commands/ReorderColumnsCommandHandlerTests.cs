using NOIR.Application.Features.Pm.Commands.ReorderColumns;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class ReorderColumnsCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ReorderColumnsCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public ReorderColumnsCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new ReorderColumnsCommandHandler(
            _dbContextMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReorderColumns()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var col1Id = Guid.NewGuid();
        var col2Id = Guid.NewGuid();

        var col1 = ProjectColumn.Create(projectId, "Todo", 0, TestTenantId);
        typeof(ProjectColumn).GetProperty("Id")!.SetValue(col1, col1Id);
        var col2 = ProjectColumn.Create(projectId, "Done", 1, TestTenantId);
        typeof(ProjectColumn).GetProperty("Id")!.SetValue(col2, col2Id);

        var columns = new List<ProjectColumn> { col1, col2 }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(columns.Object);

        // Reorder: col2 first, then col1
        var command = new ReorderColumnsCommand(projectId, new List<Guid> { col2Id, col1Id });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyProject_ShouldReturnEmptyList()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var emptyColumns = new List<ProjectColumn>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(emptyColumns.Object);

        var command = new ReorderColumnsCommand(projectId, new List<Guid>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChanges()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var colId = Guid.NewGuid();
        var col = ProjectColumn.Create(projectId, "Todo", 0, TestTenantId);
        typeof(ProjectColumn).GetProperty("Id")!.SetValue(col, colId);

        var columns = new List<ProjectColumn> { col }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(columns.Object);

        var command = new ReorderColumnsCommand(projectId, new List<Guid> { colId });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
