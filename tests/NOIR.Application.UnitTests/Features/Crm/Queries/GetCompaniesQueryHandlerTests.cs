using NOIR.Application.Features.Crm.Queries.GetCompanies;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Queries;

public class GetCompaniesQueryHandlerTests
{
    private readonly Mock<IRepository<CrmCompany, Guid>> _companyRepoMock;
    private readonly GetCompaniesQueryHandler _handler;

    private const string TestTenantId = "tenant-123";

    public GetCompaniesQueryHandlerTests()
    {
        _companyRepoMock = new Mock<IRepository<CrmCompany, Guid>>();
        _handler = new GetCompaniesQueryHandler(_companyRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ShouldReturnPagedResult()
    {
        // Arrange
        var companies = new List<CrmCompany>
        {
            CrmCompany.Create("Acme Corp", TestTenantId, industry: "Tech"),
            CrmCompany.Create("Beta Inc", TestTenantId, industry: "Finance")
        };

        _companyRepoMock
            .Setup(x => x.ListAsync(It.IsAny<CompaniesFilterSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(companies);
        _companyRepoMock
            .Setup(x => x.CountAsync(It.IsAny<CompaniesCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var query = new GetCompaniesQuery(Page: 1, PageSize: 20);

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
        _companyRepoMock
            .Setup(x => x.ListAsync(It.IsAny<CompaniesFilterSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CrmCompany>());
        _companyRepoMock
            .Setup(x => x.CountAsync(It.IsAny<CompaniesCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetCompaniesQuery(Search: "nonexistent");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WithSearchFilter_ShouldPassToSpec()
    {
        // Arrange
        _companyRepoMock
            .Setup(x => x.ListAsync(It.IsAny<CompaniesFilterSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CrmCompany>());
        _companyRepoMock
            .Setup(x => x.CountAsync(It.IsAny<CompaniesCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetCompaniesQuery(Search: "Acme", Page: 2, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _companyRepoMock.Verify(
            x => x.ListAsync(It.IsAny<CompaniesFilterSpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
