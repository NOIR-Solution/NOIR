using NOIR.Application.Features.Crm.Commands.CreatePipeline;
using NOIR.Application.Features.Crm.DTOs;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class CreatePipelineCommandHandlerTests
{
    private readonly Mock<IRepository<Pipeline, Guid>> _pipelineRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly CreatePipelineCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public CreatePipelineCommandHandlerTests()
    {
        _pipelineRepoMock = new Mock<IRepository<Pipeline, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _pipelineRepoMock
            .Setup(x => x.AddAsync(It.IsAny<Pipeline>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pipeline p, CancellationToken _) => p);

        _handler = new CreatePipelineCommandHandler(
            _pipelineRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCreatePipeline()
    {
        // Arrange
        var stages = new List<CreatePipelineStageDto>
        {
            new("Qualification", 1, "#3B82F6"),
            new("Proposal", 2, "#10B981")
        };
        var command = new CreatePipelineCommand("Sales Pipeline", false, stages);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("Sales Pipeline");
        result.Value.IsDefault.ShouldBe(false);
        result.Value.Stages.Count().ShouldBe(2);

        _pipelineRepoMock.Verify(
            x => x.AddAsync(It.IsAny<Pipeline>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DefaultPipeline_ShouldUnsetPreviousDefault()
    {
        // Arrange
        var existingDefault = Pipeline.Create("Old Default", TestTenantId, true);
        _pipelineRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DefaultPipelineSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingDefault);

        var stages = new List<CreatePipelineStageDto>
        {
            new("Stage 1", 1)
        };
        var command = new CreatePipelineCommand("New Default", true, stages);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.IsDefault.ShouldBe(true);
        existingDefault.IsDefault.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_WithStages_ShouldOrderBySort()
    {
        // Arrange
        var stages = new List<CreatePipelineStageDto>
        {
            new("Negotiation", 3, "#F59E0B"),
            new("Qualification", 1, "#3B82F6"),
            new("Proposal", 2, "#10B981")
        };
        var command = new CreatePipelineCommand("Pipeline", false, stages);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Stages.Count().ShouldBe(3);
        result.Value.Stages[0].Name.ShouldBe("Qualification");
        result.Value.Stages[1].Name.ShouldBe("Proposal");
        result.Value.Stages[2].Name.ShouldBe("Negotiation");
    }
}
