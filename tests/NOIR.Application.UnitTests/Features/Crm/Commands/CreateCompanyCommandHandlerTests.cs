using NOIR.Application.Features.Crm.Commands.CreateCompany;
using NOIR.Application.Features.Crm.Specifications;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class CreateCompanyCommandHandlerTests
{
    private readonly Mock<IRepository<CrmCompany, Guid>> _companyRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly CreateCompanyCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public CreateCompanyCommandHandlerTests()
    {
        _companyRepoMock = new Mock<IRepository<CrmCompany, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _companyRepoMock
            .Setup(x => x.AddAsync(It.IsAny<CrmCompany>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrmCompany c, CancellationToken _) => c);

        _handler = new CreateCompanyCommandHandler(
            _companyRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCreateCompany()
    {
        // Arrange
        _companyRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CompanyByNameSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrmCompany?)null);

        var command = new CreateCompanyCommand("Acme Corp", Domain: "acme.com", Industry: "Technology");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("Acme Corp");

        _companyRepoMock.Verify(
            x => x.AddAsync(It.IsAny<CrmCompany>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateName_ShouldReturnError()
    {
        // Arrange
        var existingCompany = CrmCompany.Create("Acme Corp", TestTenantId);

        _companyRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CompanyByNameSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCompany);

        var command = new CreateCompanyCommand("Acme Corp");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _companyRepoMock.Verify(
            x => x.AddAsync(It.IsAny<CrmCompany>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
