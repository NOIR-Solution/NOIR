using NOIR.Application.Features.Crm.Commands.ReorderStages;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class ReorderStagesCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly ReorderStagesCommandHandler _handler;

    private const string TestTenantId = "tenant-123";
    private readonly Guid _pipelineId = Guid.NewGuid();

    public ReorderStagesCommandHandlerTests()
    {
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new ReorderStagesCommandHandler(
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidReorder_ShouldUpdateSortOrders()
    {
        // Arrange
        var stage1 = PipelineStage.Create(_pipelineId, "Stage A", 0, TestTenantId);
        var stage2 = PipelineStage.Create(_pipelineId, "Stage B", 1, TestTenantId);
        var won = PipelineStage.CreateSystem(_pipelineId, StageType.Won, 2, TestTenantId);
        var lost = PipelineStage.CreateSystem(_pipelineId, StageType.Lost, 3, TestTenantId);

        _dbContextMock.Setup(x => x.PipelineStages)
            .Returns(new List<PipelineStage> { stage1, stage2, won, lost }.BuildMockDbSet().Object);

        // Reverse active stage order
        var command = new ReorderStagesCommand(_pipelineId, new List<Guid> { stage2.Id, stage1.Id });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        stage2.SortOrder.ShouldBe(0);
        stage1.SortOrder.ShouldBe(1);
        // System stages should be pushed to end
        won.SortOrder.ShouldBe(2);
        lost.SortOrder.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_WithSystemStageId_ShouldReturnValidationError()
    {
        // Arrange
        var stage1 = PipelineStage.Create(_pipelineId, "Stage A", 0, TestTenantId);
        var won = PipelineStage.CreateSystem(_pipelineId, StageType.Won, 1, TestTenantId);

        _dbContextMock.Setup(x => x.PipelineStages)
            .Returns(new List<PipelineStage> { stage1, won }.BuildMockDbSet().Object);

        var command = new ReorderStagesCommand(_pipelineId, new List<Guid> { stage1.Id, won.Id });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
