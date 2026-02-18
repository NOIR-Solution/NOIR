using NOIR.Application.Features.Inventory.DTOs;
using NOIR.Application.Features.Inventory.Queries.GetInventoryReceiptById;
using NOIR.Application.Features.Inventory.Specifications;
using NOIR.Domain.Entities.Inventory;

namespace NOIR.Application.UnitTests.Features.Inventory.Queries.GetInventoryReceiptById;

/// <summary>
/// Unit tests for GetInventoryReceiptByIdQueryHandler.
/// </summary>
public class GetInventoryReceiptByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<InventoryReceipt, Guid>> _repositoryMock;
    private readonly GetInventoryReceiptByIdQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetInventoryReceiptByIdQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<InventoryReceipt, Guid>>();
        _handler = new GetInventoryReceiptByIdQueryHandler(_repositoryMock.Object);
    }

    private static InventoryReceipt CreateTestReceipt(
        string receiptNumber = "RCV-20260218-0001",
        InventoryReceiptType type = InventoryReceiptType.StockIn)
    {
        var receipt = InventoryReceipt.Create(receiptNumber, type, "Test notes", TestTenantId);
        receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product 1", "Variant 1", "SKU-001", 10, 25.00m);
        return receipt;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithExistingReceipt_ShouldReturnReceiptDto()
    {
        // Arrange
        var receipt = CreateTestReceipt();
        var query = new GetInventoryReceiptByIdQuery(receipt.Id);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<InventoryReceiptByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ReceiptNumber.Should().Be("RCV-20260218-0001");
        result.Value.Type.Should().Be(InventoryReceiptType.StockIn);
        result.Value.Status.Should().Be(InventoryReceiptStatus.Draft);
        result.Value.Notes.Should().Be("Test notes");
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithConfirmedReceipt_ShouldReturnWithConfirmationDetails()
    {
        // Arrange
        var receipt = CreateTestReceipt();
        receipt.Confirm("admin-user");
        var query = new GetInventoryReceiptByIdQuery(receipt.Id);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<InventoryReceiptByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(InventoryReceiptStatus.Confirmed);
        result.Value.ConfirmedBy.Should().Be("admin-user");
        result.Value.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectTotalQuantityAndCost()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);
        receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "P1", "V1", "S1", 10, 25.00m); // 250
        receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "P2", "V2", "S2", 5, 50.00m);  // 250
        var query = new GetInventoryReceiptByIdQuery(receipt.Id);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<InventoryReceiptByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalQuantity.Should().Be(15);
        result.Value.TotalCost.Should().Be(500.00m);
        result.Value.Items.Should().HaveCount(2);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenReceiptNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetInventoryReceiptByIdQuery(Guid.NewGuid());

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<InventoryReceiptByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryReceipt?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-INVENTORY-003");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        var receipt = CreateTestReceipt();
        var query = new GetInventoryReceiptByIdQuery(receipt.Id);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<InventoryReceiptByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _repositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<InventoryReceiptByIdSpec>(), token),
            Times.Once);
    }

    #endregion
}
