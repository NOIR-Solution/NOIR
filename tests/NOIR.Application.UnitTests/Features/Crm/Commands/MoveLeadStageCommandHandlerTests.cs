using NOIR.Application.Features.Crm.Commands.MoveLeadStage;
using NOIR.Application.Features.Crm.Specifications;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class MoveLeadStageCommandHandlerTests
{
    private readonly Mock<IRepository<Lead, Guid>> _leadRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly MoveLeadStageCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public MoveLeadStageCommandHandlerTests()
    {
        _leadRepoMock = new Mock<IRepository<Lead, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new MoveLeadStageCommandHandler(
            _leadRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private Lead CreateActiveLead() =>
        Lead.Create("Test Deal", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            TestTenantId, value: 10000m);

    [Fact]
    public async Task Handle_ActiveLead_ShouldMoveToNewStage()
    {
        // Arrange
        var lead = CreateActiveLead();
        var newStageId = Guid.NewGuid();

        _leadRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LeadByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lead);

        var command = new MoveLeadStageCommand(lead.Id, newStageId, 2.5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        lead.StageId.ShouldBe(newStageId);
        lead.SortOrder.ShouldBe(2.5);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WonLead_ShouldReturnError()
    {
        // Arrange
        var lead = CreateActiveLead();
        lead.Win();

        _leadRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LeadByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lead);

        var command = new MoveLeadStageCommand(lead.Id, Guid.NewGuid(), 1.0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LostLead_ShouldReturnError()
    {
        // Arrange
        var lead = CreateActiveLead();
        lead.Lose("Budget");

        _leadRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LeadByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lead);

        var command = new MoveLeadStageCommand(lead.Id, Guid.NewGuid(), 1.0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
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

    [Fact]
    public async Task Handle_SameStage_ShouldSucceedAndUpdateSortOrder()
    {
        // Arrange
        var lead = CreateActiveLead();
        var currentStageId = lead.StageId;

        _leadRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LeadByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lead);

        // Move to the same stage but with a different sort order
        var command = new MoveLeadStageCommand(lead.Id, currentStageId, 5.0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        lead.StageId.ShouldBe(currentStageId);
        lead.SortOrder.ShouldBe(5.0);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
