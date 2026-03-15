using NOIR.Application.Features.Crm.Commands.UpdatePipeline;
using NOIR.Application.Features.Crm.DTOs;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class UpdatePipelineCommandHandlerTests
{
    private readonly Mock<IRepository<Pipeline, Guid>> _pipelineRepoMock;
    private readonly Mock<IRepository<Lead, Guid>> _leadRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly UpdatePipelineCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public UpdatePipelineCommandHandlerTests()
    {
        _pipelineRepoMock = new Mock<IRepository<Pipeline, Guid>>();
        _leadRepoMock = new Mock<IRepository<Lead, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _dbContextMock = new Mock<IApplicationDbContext>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new UpdatePipelineCommandHandler(
            _pipelineRepoMock.Object,
            _leadRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _dbContextMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingPipeline_ShouldUpdate()
    {
        // Arrange
        var pipeline = Pipeline.Create("Old Pipeline", TestTenantId);
        var stage = PipelineStage.Create(pipeline.Id, "Old Stage", 1, TestTenantId, "#3B82F6");
        pipeline.Stages.Add(stage);

        _pipelineRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PipelineByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipeline);
        _leadRepoMock
            .Setup(x => x.CountAsync(It.IsAny<ActiveLeadsByStageSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var stages = new List<UpdatePipelineStageDto>
        {
            new(null, "New Stage", 1, "#10B981")
        };
        var command = new UpdatePipelineCommand(pipeline.Id, "Updated Pipeline", false, stages);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("Updated Pipeline");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentPipeline_ShouldReturnNotFound()
    {
        // Arrange
        _pipelineRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PipelineByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pipeline?)null);

        var stages = new List<UpdatePipelineStageDto>
        {
            new(null, "Stage", 1, "#3B82F6")
        };
        var command = new UpdatePipelineCommand(Guid.NewGuid(), "Pipeline", false, stages);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RemoveStageWithActiveLeads_ShouldFail()
    {
        // Arrange
        var pipeline = Pipeline.Create("Pipeline", TestTenantId);
        var stage = PipelineStage.Create(pipeline.Id, "Active Stage", 1, TestTenantId);
        pipeline.Stages.Add(stage);

        _pipelineRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PipelineByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipeline);
        _leadRepoMock
            .Setup(x => x.CountAsync(It.IsAny<ActiveLeadsByStageSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Command removes existing stage (not in commandStageIds)
        var stages = new List<UpdatePipelineStageDto>
        {
            new(null, "New Stage", 1, "#3B82F6")
        };
        var command = new UpdatePipelineCommand(pipeline.Id, "Pipeline", false, stages);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SetAsDefault_ShouldUnsetPreviousDefault()
    {
        // Arrange
        var currentDefault = Pipeline.Create("Default", TestTenantId, true);
        var pipeline = Pipeline.Create("Other", TestTenantId, false);
        var stage = PipelineStage.Create(pipeline.Id, "Stage", 1, TestTenantId);
        pipeline.Stages.Add(stage);

        _pipelineRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PipelineByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipeline);
        _pipelineRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DefaultPipelineSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentDefault);

        var stages = new List<UpdatePipelineStageDto>
        {
            new(stage.Id, "Stage", 1, "#3B82F6")
        };
        var command = new UpdatePipelineCommand(pipeline.Id, "Other", true, stages);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        currentDefault.IsDefault.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_RemoveMultipleStages_OnlyBlocksIfAnyHasLeads()
    {
        // Arrange — pipeline with two stages, one has active leads
        var pipeline = Pipeline.Create("Pipeline", TestTenantId);
        var stageWithLeads = PipelineStage.Create(pipeline.Id, "Has Leads", 1, TestTenantId);
        var stageWithoutLeads = PipelineStage.Create(pipeline.Id, "No Leads", 2, TestTenantId);
        pipeline.Stages.Add(stageWithLeads);
        pipeline.Stages.Add(stageWithoutLeads);

        _pipelineRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PipelineByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipeline);

        // First stage to be removed (stageWithoutLeads) has 0 leads, second (stageWithLeads) has 2
        // The handler iterates stagesToRemove and checks each — order depends on LINQ
        // We set up CountAsync to return different values based on the stage being checked
        var callCount = 0;
        _leadRepoMock
            .Setup(x => x.CountAsync(It.IsAny<ActiveLeadsByStageSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                // At least one stage has active leads — handler should fail
                return callCount == 1 ? 0 : 2;
            });

        // Command removes both existing stages by providing no matching stage IDs
        var stages = new List<UpdatePipelineStageDto>
        {
            new(null, "Brand New Stage", 1, "#10B981")
        };
        var command = new UpdatePipelineCommand(pipeline.Id, "Pipeline", false, stages);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — should fail because one of the removed stages has active leads
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RemoveMultipleStages_AllWithoutLeads_ShouldSucceed()
    {
        // Arrange — pipeline with two stages, none have active leads
        var pipeline = Pipeline.Create("Pipeline", TestTenantId);
        var stage1 = PipelineStage.Create(pipeline.Id, "Stage A", 1, TestTenantId);
        var stage2 = PipelineStage.Create(pipeline.Id, "Stage B", 2, TestTenantId);
        pipeline.Stages.Add(stage1);
        pipeline.Stages.Add(stage2);

        _pipelineRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PipelineByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipeline);

        _leadRepoMock
            .Setup(x => x.CountAsync(It.IsAny<ActiveLeadsByStageSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Command removes both existing stages
        var stages = new List<UpdatePipelineStageDto>
        {
            new(null, "Replacement Stage", 1, "#3B82F6")
        };
        var command = new UpdatePipelineCommand(pipeline.Id, "Pipeline", false, stages);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
