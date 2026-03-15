using NOIR.Application.Features.Crm.Commands.DeleteCompany;
using NOIR.Application.Features.Crm.Specifications;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class DeleteCompanyCommandHandlerTests
{
    private readonly Mock<IRepository<CrmCompany, Guid>> _companyRepoMock;
    private readonly Mock<IRepository<CrmContact, Guid>> _contactRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly DeleteCompanyCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public DeleteCompanyCommandHandlerTests()
    {
        _companyRepoMock = new Mock<IRepository<CrmCompany, Guid>>();
        _contactRepoMock = new Mock<IRepository<CrmContact, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new DeleteCompanyCommandHandler(
            _companyRepoMock.Object,
            _contactRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_HasContacts_ShouldReturnError()
    {
        // Arrange
        var company = CrmCompany.Create("Acme Corp", TestTenantId);

        _companyRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CompanyByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        _contactRepoMock
            .Setup(x => x.CountAsync(It.IsAny<CompanyHasContactsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var command = new DeleteCompanyCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _companyRepoMock.Verify(x => x.Remove(It.IsAny<CrmCompany>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoContacts_ShouldDelete()
    {
        // Arrange
        var company = CrmCompany.Create("Acme Corp", TestTenantId);

        _companyRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CompanyByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        _contactRepoMock
            .Setup(x => x.CountAsync(It.IsAny<CompanyHasContactsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var command = new DeleteCompanyCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _companyRepoMock.Verify(x => x.Remove(company), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CompanyNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _companyRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CompanyByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrmCompany?)null);

        var command = new DeleteCompanyCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
    }
}
