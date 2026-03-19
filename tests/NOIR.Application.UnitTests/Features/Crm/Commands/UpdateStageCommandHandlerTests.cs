using NOIR.Application.Features.Crm.Commands.UpdateStage;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class UpdateStageCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly UpdateStageCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public UpdateStageCommandHandlerTests()
    {
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new UpdateStageCommandHandler(
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ActiveStage_ShouldUpdateNameAndColor()
    {
        // Arrange
        var stage = PipelineStage.Create(Guid.NewGuid(), "Old Name", 0, TestTenantId, "#6366f1");
        _dbContextMock.Setup(x => x.PipelineStages)
            .Returns(new List<PipelineStage> { stage }.BuildMockDbSet().Object);

        var command = new UpdateStageCommand(stage.Id, "New Name", "#22c55e") { AuditUserId = "user-1" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value!.Name.ShouldBe("New Name");
        result.Value.Color.ShouldBe("#22c55e");
    }

    [Fact]
    public async Task Handle_SystemStage_ShouldOnlyUpdateColor()
    {
        // Arrange
        var stage = PipelineStage.CreateSystem(Guid.NewGuid(), StageType.Won, 99, TestTenantId);
        _dbContextMock.Setup(x => x.PipelineStages)
            .Returns(new List<PipelineStage> { stage }.BuildMockDbSet().Object);

        var command = new UpdateStageCommand(stage.Id, "Attempted Rename", "#ff0000") { AuditUserId = "user-1" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value!.Name.ShouldBe("Won"); // Name unchanged
        result.Value.Color.ShouldBe("#ff0000"); // Color updated
    }

    [Fact]
    public async Task Handle_StageNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _dbContextMock.Setup(x => x.PipelineStages)
            .Returns(new List<PipelineStage>().BuildMockDbSet().Object);

        var command = new UpdateStageCommand(Guid.NewGuid(), "Name", "#fff") { AuditUserId = "user-1" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
