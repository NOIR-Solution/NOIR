using NOIR.Application.Features.Crm.Queries.GetCrmDashboard;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Queries;

public class GetCrmDashboardQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;

    public GetCrmDashboardQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
    }

    [Fact]
    public async Task Handle_EmptyData_ShouldReturnZeroMetrics()
    {
        // Arrange
        var contacts = new List<CrmContact>().BuildMockDbSet();
        var companies = new List<CrmCompany>().BuildMockDbSet();
        var leads = new List<Lead>().BuildMockDbSet();

        _dbContextMock.Setup(x => x.CrmContacts).Returns(contacts.Object);
        _dbContextMock.Setup(x => x.CrmCompanies).Returns(companies.Object);
        _dbContextMock.Setup(x => x.Leads).Returns(leads.Object);

        var handler = new GetCrmDashboardQueryHandler(_dbContextMock.Object);
        var query = new GetCrmDashboardQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.TotalContacts.ShouldBe(0);
        result.Value.TotalCompanies.ShouldBe(0);
        result.Value.ActiveLeads.ShouldBe(0);
        result.Value.WonLeads.ShouldBe(0);
        result.Value.LostLeads.ShouldBe(0);
        result.Value.TotalPipelineValue.ShouldBe(0);
        result.Value.WonDealValue.ShouldBe(0);
        result.Value.ConversionRate.ShouldBe(0);
    }
}
