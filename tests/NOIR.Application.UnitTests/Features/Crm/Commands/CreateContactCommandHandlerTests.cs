using NOIR.Application.Features.Crm.Commands.CreateContact;
using NOIR.Application.Features.Crm.Specifications;
using NOIR.Domain.Entities.Crm;
using CrmContactDto = NOIR.Application.Features.Crm.DTOs.ContactDto;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class CreateContactCommandHandlerTests
{
    private readonly Mock<IRepository<CrmContact, Guid>> _contactRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly CreateContactCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public CreateContactCommandHandlerTests()
    {
        _contactRepoMock = new Mock<IRepository<CrmContact, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _contactRepoMock
            .Setup(x => x.AddAsync(It.IsAny<CrmContact>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrmContact c, CancellationToken _) => c);

        _handler = new CreateContactCommandHandler(
            _contactRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCreateContact()
    {
        // Arrange
        _contactRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ContactByEmailSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrmContact?)null);

        var command = new CreateContactCommand(
            "John", "Doe", "john@example.com", ContactSource.Web,
            Phone: "555-0100", JobTitle: "CTO");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("John");
        result.Value.LastName.Should().Be("Doe");
        result.Value.Email.Should().Be("john@example.com");

        _contactRepoMock.Verify(
            x => x.AddAsync(It.IsAny<CrmContact>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ShouldReturnError()
    {
        // Arrange
        var existingContact = CrmContact.Create(
            "Existing", "User", "john@example.com", ContactSource.Web, TestTenantId);

        _contactRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ContactByEmailSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContact);

        var command = new CreateContactCommand(
            "John", "Doe", "john@example.com", ContactSource.Web);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _contactRepoMock.Verify(
            x => x.AddAsync(It.IsAny<CrmContact>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
