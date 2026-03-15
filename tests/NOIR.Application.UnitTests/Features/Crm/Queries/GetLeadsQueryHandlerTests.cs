using NOIR.Application.Features.Crm.Queries.GetLeads;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Queries;

public class GetLeadsQueryHandlerTests
{
    private readonly Mock<IRepository<Lead, Guid>> _leadRepoMock;
    private readonly GetLeadsQueryHandler _handler;

    private const string TestTenantId = "tenant-123";

    public GetLeadsQueryHandlerTests()
    {
        _leadRepoMock = new Mock<IRepository<Lead, Guid>>();
        _handler = new GetLeadsQueryHandler(_leadRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ShouldReturnPagedResult()
    {
        // Arrange
        var pipelineId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var leads = new List<Lead>
        {
            Lead.Create("Deal 1", Guid.NewGuid(), pipelineId, stageId, TestTenantId, value: 10000),
            Lead.Create("Deal 2", Guid.NewGuid(), pipelineId, stageId, TestTenantId, value: 20000)
        };

        _leadRepoMock
            .Setup(x => x.ListAsync(It.IsAny<LeadsFilterSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(leads);
        _leadRepoMock
            .Setup(x => x.CountAsync(It.IsAny<LeadsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var query = new GetLeadsQuery(Page: 1, PageSize: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(2);
        result.Value.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_EmptyResult_ShouldReturnEmptyList()
    {
        // Arrange
        _leadRepoMock
            .Setup(x => x.ListAsync(It.IsAny<LeadsFilterSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Lead>());
        _leadRepoMock
            .Setup(x => x.CountAsync(It.IsAny<LeadsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetLeadsQuery(Status: LeadStatus.Won);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WithFilters_ShouldPassToSpec()
    {
        // Arrange
        var pipelineId = Guid.NewGuid();
        _leadRepoMock
            .Setup(x => x.ListAsync(It.IsAny<LeadsFilterSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Lead>());
        _leadRepoMock
            .Setup(x => x.CountAsync(It.IsAny<LeadsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetLeadsQuery(
            PipelineId: pipelineId,
            Status: LeadStatus.Active,
            Page: 2, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _leadRepoMock.Verify(
            x => x.ListAsync(It.IsAny<LeadsFilterSpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
