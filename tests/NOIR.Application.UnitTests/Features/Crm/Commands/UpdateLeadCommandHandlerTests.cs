using NOIR.Application.Features.Crm.Commands.UpdateLead;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class UpdateLeadCommandHandlerTests
{
    private readonly Mock<IRepository<Lead, Guid>> _leadRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly UpdateLeadCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public UpdateLeadCommandHandlerTests()
    {
        _leadRepoMock = new Mock<IRepository<Lead, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new UpdateLeadCommandHandler(
            _leadRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingLead_ShouldUpdate()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var lead = Lead.Create(
            "Original Deal", contactId, Guid.NewGuid(), Guid.NewGuid(),
            TestTenantId, value: 10000, currency: "USD");

        _leadRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LeadByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lead);

        var newContactId = Guid.NewGuid();
        var command = new UpdateLeadCommand(
            lead.Id, "Updated Deal", newContactId,
            Value: 25000, Currency: "EUR",
            Notes: "Updated notes");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        lead.Title.ShouldBe("Updated Deal");
        lead.ContactId.ShouldBe(newContactId);
        lead.Value.ShouldBe(25000);
        lead.Currency.ShouldBe("EUR");
        lead.Notes.ShouldBe("Updated notes");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentLead_ShouldReturnNotFound()
    {
        // Arrange
        _leadRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LeadByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lead?)null);

        var command = new UpdateLeadCommand(
            Guid.NewGuid(), "Deal", Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
