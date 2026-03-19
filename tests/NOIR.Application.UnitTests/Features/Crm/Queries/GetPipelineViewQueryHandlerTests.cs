using NOIR.Application.Features.Crm.Queries.GetPipelineView;
using NOIR.Application.Features.Crm.Specifications;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Queries;

public class GetPipelineViewQueryHandlerTests
{
    private readonly Mock<IRepository<Pipeline, Guid>> _pipelineRepoMock;
    private readonly Mock<IRepository<Lead, Guid>> _leadRepoMock;
    private readonly GetPipelineViewQueryHandler _handler;

    private const string TestTenantId = "tenant-123";

    public GetPipelineViewQueryHandlerTests()
    {
        _pipelineRepoMock = new Mock<IRepository<Pipeline, Guid>>();
        _leadRepoMock = new Mock<IRepository<Lead, Guid>>();

        _handler = new GetPipelineViewQueryHandler(
            _pipelineRepoMock.Object,
            _leadRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnStagesWithActiveLeads()
    {
        // Arrange
        var pipeline = Pipeline.Create("Sales Pipeline", TestTenantId, isDefault: true);
        var stage1 = PipelineStage.Create(pipeline.Id, "New", 0, TestTenantId, "#6B7280");
        var stage2 = PipelineStage.Create(pipeline.Id, "Qualified", 1, TestTenantId, "#3B82F6");
        pipeline.Stages.Add(stage1);
        pipeline.Stages.Add(stage2);

        _pipelineRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PipelineByIdWithLeadsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipeline);

        // Active leads only
        var activeLead = Lead.Create(
            "Active Deal", Guid.NewGuid(), pipeline.Id, stage1.Id, TestTenantId, value: 5000m);

        _leadRepoMock
            .Setup(x => x.ListAsync(It.IsAny<LeadsByPipelineSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Lead> { activeLead });

        var query = new GetPipelineViewQuery(pipeline.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Id.ShouldBe(pipeline.Id);
        result.Value.Stages.Count().ShouldBe(2);
        result.Value.Stages[0].Name.ShouldBe("New");
        result.Value.Stages[0].Leads.Count().ShouldBe(1);
        result.Value.Stages[1].Name.ShouldBe("Qualified");
        result.Value.Stages[1].Leads.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldShowWonLeadsInWonSystemStage()
    {
        // Arrange: pipeline with 1 active stage + Won/Lost system stages
        var pipeline = Pipeline.Create("Sales Pipeline", TestTenantId, isDefault: true);
        var stage = PipelineStage.Create(pipeline.Id, "New", 0, TestTenantId, "#6B7280");
        var won = PipelineStage.CreateSystem(pipeline.Id, StageType.Won, 1, TestTenantId);
        var lost = PipelineStage.CreateSystem(pipeline.Id, StageType.Lost, 2, TestTenantId);
        pipeline.Stages.Add(stage);
        pipeline.Stages.Add(won);
        pipeline.Stages.Add(lost);

        _pipelineRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PipelineByIdWithLeadsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipeline);

        var activeLead = Lead.Create(
            "Active", Guid.NewGuid(), pipeline.Id, stage.Id, TestTenantId, value: 1000m);
        var wonLead = Lead.Create(
            "Won Deal", Guid.NewGuid(), pipeline.Id, stage.Id, TestTenantId, value: 5000m);
        wonLead.Win();

        _leadRepoMock
            .Setup(x => x.ListAsync(It.IsAny<LeadsByPipelineSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Lead> { activeLead, wonLead });

        var query = new GetPipelineViewQuery(pipeline.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert: active stage has 1 lead; Won system stage has the won lead
        result.IsSuccess.ShouldBe(true);
        result.Value.Stages.Count().ShouldBe(3); // 1 active + Won + Lost
        result.Value.Stages[0].Name.ShouldBe("New");
        result.Value.Stages[0].Leads.Count().ShouldBe(1);
        result.Value.Stages[1].IsSystem.ShouldBe(true);
        result.Value.Stages[1].Leads.Count().ShouldBe(1); // won lead
        result.Value.Stages[2].IsSystem.ShouldBe(true);
        result.Value.Stages[2].Leads.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_PipelineNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _pipelineRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PipelineByIdWithLeadsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pipeline?)null);

        var query = new GetPipelineViewQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
    }
}
