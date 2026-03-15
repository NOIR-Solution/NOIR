using NOIR.Application.Features.Crm.Queries.GetCompanyById;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Queries;

public class GetCompanyByIdQueryHandlerTests
{
    private readonly Mock<IRepository<CrmCompany, Guid>> _companyRepoMock;
    private readonly GetCompanyByIdQueryHandler _handler;

    private const string TestTenantId = "tenant-123";

    public GetCompanyByIdQueryHandlerTests()
    {
        _companyRepoMock = new Mock<IRepository<CrmCompany, Guid>>();
        _handler = new GetCompanyByIdQueryHandler(_companyRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingCompany_ShouldReturnDto()
    {
        // Arrange
        var company = CrmCompany.Create(
            "Acme Corp", TestTenantId,
            domain: "acme.com",
            industry: "Technology",
            phone: "555-0100");

        _companyRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CompanyByIdReadOnlySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        var query = new GetCompanyByIdQuery(company.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Id.ShouldBe(company.Id);
        result.Value.Name.ShouldBe("Acme Corp");
        result.Value.Industry.ShouldBe("Technology");
    }

    [Fact]
    public async Task Handle_NonExistentCompany_ShouldReturnNotFound()
    {
        // Arrange
        _companyRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CompanyByIdReadOnlySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrmCompany?)null);

        var query = new GetCompanyByIdQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
    }
}
