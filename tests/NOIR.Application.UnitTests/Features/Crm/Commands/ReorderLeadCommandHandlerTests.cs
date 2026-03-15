using NOIR.Application.Features.Crm.Commands.ReorderLead;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class ReorderLeadCommandHandlerTests
{
    private readonly Mock<IRepository<Lead, Guid>> _leadRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ReorderLeadCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public ReorderLeadCommandHandlerTests()
    {
        _leadRepoMock = new Mock<IRepository<Lead, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new ReorderLeadCommandHandler(
            _leadRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingLead_ShouldReorder()
    {
        // Arrange
        var stageId = Guid.NewGuid();
        var lead = Lead.Create(
            "Enterprise Deal", Guid.NewGuid(), Guid.NewGuid(), stageId,
            TestTenantId, sortOrder: 1.0);

        _leadRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LeadByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lead);

        var command = new ReorderLeadCommand(lead.Id, 2.5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        lead.SortOrder.ShouldBe(2.5);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentLead_ShouldReturnNotFound()
    {
        // Arrange
        _leadRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LeadByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lead?)null);

        var command = new ReorderLeadCommand(Guid.NewGuid(), 1.0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
