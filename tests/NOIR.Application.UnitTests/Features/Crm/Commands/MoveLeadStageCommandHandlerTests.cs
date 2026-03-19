using NOIR.Application.Features.Crm.Commands.MoveLeadStage;
using NOIR.Application.Features.Crm.Specifications;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class MoveLeadStageCommandHandlerTests
{
    private readonly Mock<IRepository<Lead, Guid>> _leadRepoMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly MoveLeadStageCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public MoveLeadStageCommandHandlerTests()
    {
        _leadRepoMock = new Mock<IRepository<Lead, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new MoveLeadStageCommandHandler(
            _leadRepoMock.Object,
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private Lead CreateActiveLead() =>
        Lead.Create("Test Deal", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            TestTenantId, value: 10000m);

    // Returns the stage so the test can use stage.Id for the command
    private PipelineStage SetupActiveStage()
    {
        var stage = PipelineStage.Create(Guid.NewGuid(), "Active Stage", 0, TestTenantId, "#6366f1");
        var mockDbSet = new List<PipelineStage> { stage }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.PipelineStages).Returns(mockDbSet.Object);
        return stage;
    }

    private PipelineStage SetupSystemStage(StageType stageType)
    {
        var stage = PipelineStage.CreateSystem(Guid.NewGuid(), stageType, 99, TestTenantId);
        var mockDbSet = new List<PipelineStage> { stage }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.PipelineStages).Returns(mockDbSet.Object);
        return stage;
    }

    [Fact]
    public async Task Handle_SameStage_ShouldSucceedAndUpdateSortOrder()
    {
        // Arrange
        var lead = CreateActiveLead();
        var currentStageId = lead.StageId;

        _leadRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LeadByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lead);

        // Same-stage reorder — no dbContext needed
        var command = new MoveLeadStageCommand(lead.Id, currentStageId, 5.0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        lead.StageId.ShouldBe(currentStageId);
        lead.SortOrder.ShouldBe(5.0);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ActiveLead_CrossStageMove_ShouldSucceed()
    {
        // Arrange
        var lead = CreateActiveLead();
        var targetStage = SetupActiveStage();

        _leadRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LeadByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lead);

        var command = new MoveLeadStageCommand(lead.Id, targetStage.Id, 2.5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        lead.StageId.ShouldBe(targetStage.Id);
        lead.SortOrder.ShouldBe(2.5);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WonLead_MoveToActiveStage_ShouldReturnValidationError()
    {
        // Arrange
        var lead = CreateActiveLead();
        lead.Win();
        var targetStage = SetupActiveStage();

        _leadRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LeadByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lead);

        var command = new MoveLeadStageCommand(lead.Id, targetStage.Id, 1.0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — must use ReopenLead first
        result.IsSuccess.ShouldBe(false);
        lead.Status.ShouldBe(LeadStatus.Won);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LostLead_MoveToActiveStage_ShouldReturnValidationError()
    {
        // Arrange
        var lead = CreateActiveLead();
        lead.Lose("Budget");
        var targetStage = SetupActiveStage();

        _leadRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LeadByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lead);

        var command = new MoveLeadStageCommand(lead.Id, targetStage.Id, 1.0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — must use ReopenLead first
        result.IsSuccess.ShouldBe(false);
        lead.Status.ShouldBe(LeadStatus.Lost);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CrossStageToWonStage_ShouldReturnValidationError()
    {
        // Arrange
        var lead = CreateActiveLead();
        var wonStage = SetupSystemStage(StageType.Won);

        _leadRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LeadByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lead);

        var command = new MoveLeadStageCommand(lead.Id, wonStage.Id, 1.0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CrossStageToLostStage_ShouldReturnValidationError()
    {
        // Arrange
        var lead = CreateActiveLead();
        var lostStage = SetupSystemStage(StageType.Lost);

        _leadRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LeadByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lead);

        var command = new MoveLeadStageCommand(lead.Id, lostStage.Id, 1.0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LeadNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _leadRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LeadByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lead?)null);

        var command = new MoveLeadStageCommand(Guid.NewGuid(), Guid.NewGuid(), 1.0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
    }
}
