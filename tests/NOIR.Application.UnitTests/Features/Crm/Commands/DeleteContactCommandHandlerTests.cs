using NOIR.Application.Features.Crm.Commands.DeleteContact;
using NOIR.Application.Features.Crm.Specifications;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class DeleteContactCommandHandlerTests
{
    private readonly Mock<IRepository<CrmContact, Guid>> _contactRepoMock;
    private readonly Mock<IRepository<Lead, Guid>> _leadRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly DeleteContactCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public DeleteContactCommandHandlerTests()
    {
        _contactRepoMock = new Mock<IRepository<CrmContact, Guid>>();
        _leadRepoMock = new Mock<IRepository<Lead, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new DeleteContactCommandHandler(
            _contactRepoMock.Object,
            _leadRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_NoActiveLeads_ShouldDeleteContact()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contact = CrmContact.Create(
            "John", "Doe", "john@example.com", ContactSource.Web, TestTenantId);

        _contactRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ContactByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);

        _leadRepoMock
            .Setup(x => x.CountAsync(It.IsAny<ContactHasActiveLeadsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var command = new DeleteContactCommand(contactId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _contactRepoMock.Verify(x => x.Remove(contact), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_HasActiveLeads_ShouldReturnError()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contact = CrmContact.Create(
            "John", "Doe", "john@example.com", ContactSource.Web, TestTenantId);

        _contactRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ContactByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);

        _leadRepoMock
            .Setup(x => x.CountAsync(It.IsAny<ContactHasActiveLeadsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var command = new DeleteContactCommand(contactId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _contactRepoMock.Verify(x => x.Remove(It.IsAny<CrmContact>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ContactNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _contactRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ContactByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrmContact?)null);

        var command = new DeleteContactCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
    }
}
