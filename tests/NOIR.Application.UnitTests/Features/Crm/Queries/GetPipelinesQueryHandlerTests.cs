using NOIR.Application.Features.Crm.Queries.GetPipelines;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Queries;

public class GetPipelinesQueryHandlerTests
{
    private readonly Mock<IRepository<Pipeline, Guid>> _pipelineRepoMock;
    private readonly GetPipelinesQueryHandler _handler;

    private const string TestTenantId = "tenant-123";

    public GetPipelinesQueryHandlerTests()
    {
        _pipelineRepoMock = new Mock<IRepository<Pipeline, Guid>>();
        _handler = new GetPipelinesQueryHandler(_pipelineRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WithPipelines_ShouldReturnList()
    {
        // Arrange
        var pipeline1 = Pipeline.Create("Sales", TestTenantId, true);
        var stage1 = PipelineStage.Create(pipeline1.Id, "Qualification", 1, TestTenantId, "#3B82F6");
        var stage2 = PipelineStage.Create(pipeline1.Id, "Proposal", 2, TestTenantId, "#10B981");
        pipeline1.Stages.Add(stage1);
        pipeline1.Stages.Add(stage2);

        var pipeline2 = Pipeline.Create("Support", TestTenantId);

        _pipelineRepoMock
            .Setup(x => x.ListAsync(It.IsAny<PipelinesListSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Pipeline> { pipeline1, pipeline2 });

        var query = new GetPipelinesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Count().ShouldBe(2);
        result.Value[0].Name.ShouldBe("Sales");
        result.Value[0].IsDefault.ShouldBe(true);
        result.Value[0].Stages.Count().ShouldBe(2);
        result.Value[0].Stages[0].Name.ShouldBe("Qualification");
    }

    [Fact]
    public async Task Handle_NoPipelines_ShouldReturnEmptyList()
    {
        // Arrange
        _pipelineRepoMock
            .Setup(x => x.ListAsync(It.IsAny<PipelinesListSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Pipeline>());

        var query = new GetPipelinesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_StagesOrderedBySortOrder()
    {
        // Arrange
        var pipeline = Pipeline.Create("Sales", TestTenantId);
        var stage3 = PipelineStage.Create(pipeline.Id, "Negotiation", 3, TestTenantId, "#F59E0B");
        var stage1 = PipelineStage.Create(pipeline.Id, "Qualification", 1, TestTenantId, "#3B82F6");
        var stage2 = PipelineStage.Create(pipeline.Id, "Proposal", 2, TestTenantId, "#10B981");
        pipeline.Stages.Add(stage3);
        pipeline.Stages.Add(stage1);
        pipeline.Stages.Add(stage2);

        _pipelineRepoMock
            .Setup(x => x.ListAsync(It.IsAny<PipelinesListSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Pipeline> { pipeline });

        var query = new GetPipelinesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value[0].Stages[0].Name.ShouldBe("Qualification");
        result.Value[0].Stages[1].Name.ShouldBe("Proposal");
        result.Value[0].Stages[2].Name.ShouldBe("Negotiation");
    }
}
