using NOIR.Application.Features.Crm.Commands.CreateStage;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class CreateStageCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly CreateStageCommandHandler _handler;

    private const string TestTenantId = "tenant-123";
    private readonly Guid _pipelineId = Guid.NewGuid();

    public CreateStageCommandHandlerTests()
    {
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new CreateStageCommandHandler(
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private void SetupEmptyPipeline()
    {
        var stages = new List<PipelineStage>();
        _dbContextMock.Setup(x => x.PipelineStages).Returns(stages.BuildMockDbSet().Object);
    }

    private void SetupPipelineWithStages()
    {
        var active = PipelineStage.Create(_pipelineId, "Existing Stage", 0, TestTenantId);
        var won = PipelineStage.CreateSystem(_pipelineId, StageType.Won, 1, TestTenantId);
        var lost = PipelineStage.CreateSystem(_pipelineId, StageType.Lost, 2, TestTenantId);
        var stages = new List<PipelineStage> { active, won, lost };
        _dbContextMock.Setup(x => x.PipelineStages).Returns(stages.BuildMockDbSet().Object);
    }

    [Fact]
    public async Task Handle_EmptyPipeline_ShouldCreateStageAtSortOrder0()
    {
        // Arrange
        SetupEmptyPipeline();
        var command = new CreateStageCommand(_pipelineId, "New Stage", "#3b82f6") { AuditUserId = "user-1" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value!.Name.ShouldBe("New Stage");
        result.Value.SortOrder.ShouldBe(0);
        result.Value.IsSystem.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PipelineWithSystemStages_ShouldInsertBeforeSystemStages()
    {
        // Arrange
        SetupPipelineWithStages();
        var command = new CreateStageCommand(_pipelineId, "Added Stage", "#6366f1") { AuditUserId = "user-1" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        // New stage gets SortOrder = 1 (lastActiveOrder=0, so +1)
        result.Value!.SortOrder.ShouldBe(1);
    }
}
