using NOIR.Application.Features.Crm.Commands.UpdateCompany;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class UpdateCompanyCommandHandlerTests
{
    private readonly Mock<IRepository<CrmCompany, Guid>> _companyRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly UpdateCompanyCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public UpdateCompanyCommandHandlerTests()
    {
        _companyRepoMock = new Mock<IRepository<CrmCompany, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new UpdateCompanyCommandHandler(
            _companyRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingCompany_ShouldUpdate()
    {
        // Arrange
        var company = CrmCompany.Create("Acme Corp", TestTenantId, industry: "Technology");

        _companyRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CompanyByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        _companyRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CompanyByNameSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrmCompany?)null);

        var command = new UpdateCompanyCommand(
            company.Id, "Acme Corp Updated",
            Domain: "acme.com",
            Industry: "Software",
            Phone: "555-0200");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        company.Name.Should().Be("Acme Corp Updated");
        company.Industry.Should().Be("Software");
        company.Phone.Should().Be("555-0200");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentCompany_ShouldReturnNotFound()
    {
        // Arrange
        _companyRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CompanyByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrmCompany?)null);

        var command = new UpdateCompanyCommand(Guid.NewGuid(), "Test");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateName_ShouldReturnConflict()
    {
        // Arrange
        var company = CrmCompany.Create("Acme Corp", TestTenantId);
        var otherCompany = CrmCompany.Create("Other Corp", TestTenantId);

        _companyRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CompanyByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        _companyRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CompanyByNameSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherCompany);

        var command = new UpdateCompanyCommand(company.Id, "Other Corp");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
