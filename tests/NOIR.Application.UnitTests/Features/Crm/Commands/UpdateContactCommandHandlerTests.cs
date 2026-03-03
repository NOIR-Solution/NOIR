using NOIR.Application.Features.Crm.Commands.UpdateContact;
using NOIR.Application.Features.Crm.Specifications;
using NOIR.Domain.Entities.Crm;
using CrmContactDto = NOIR.Application.Features.Crm.DTOs.ContactDto;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class UpdateContactCommandHandlerTests
{
    private readonly Mock<IRepository<CrmContact, Guid>> _contactRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly UpdateContactCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public UpdateContactCommandHandlerTests()
    {
        _contactRepoMock = new Mock<IRepository<CrmContact, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new UpdateContactCommandHandler(
            _contactRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldUpdateContact()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contact = CrmContact.Create(
            "John", "Doe", "john@example.com", ContactSource.Web, TestTenantId);

        _contactRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ContactByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);

        _contactRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ContactByEmailSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrmContact?)null);

        var command = new UpdateContactCommand(
            contactId, "Jane", "Smith", "jane@example.com", ContactSource.Referral);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("Jane");
        result.Value.LastName.Should().Be("Smith");
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ContactNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _contactRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ContactByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrmContact?)null);

        var command = new UpdateContactCommand(
            Guid.NewGuid(), "Jane", "Smith", "jane@example.com", ContactSource.Web);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
