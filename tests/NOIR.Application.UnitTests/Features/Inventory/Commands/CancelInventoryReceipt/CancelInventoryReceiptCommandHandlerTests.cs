using NOIR.Application.Features.Inventory.Commands.CancelInventoryReceipt;
using NOIR.Application.Features.Inventory.DTOs;
using NOIR.Application.Features.Inventory.Specifications;
using NOIR.Domain.Entities.Inventory;

namespace NOIR.Application.UnitTests.Features.Inventory.Commands.CancelInventoryReceipt;

/// <summary>
/// Unit tests for CancelInventoryReceiptCommandHandler.
/// Tests receipt cancellation scenarios.
/// </summary>
public class CancelInventoryReceiptCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<InventoryReceipt, Guid>> _receiptRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CancelInventoryReceiptCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public CancelInventoryReceiptCommandHandlerTests()
    {
        _receiptRepositoryMock = new Mock<IRepository<InventoryReceipt, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new CancelInventoryReceiptCommandHandler(
            _receiptRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static InventoryReceipt CreateDraftReceipt(
        string receiptNumber = "RCV-20260218-0001")
    {
        return InventoryReceipt.Create(receiptNumber, InventoryReceiptType.StockIn, "Test", TestTenantId);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDraftReceipt_ShouldCancelSuccessfully()
    {
        // Arrange
        var receipt = CreateDraftReceipt();
        var command = new CancelInventoryReceiptCommand(receipt.Id, "No longer needed") { UserId = "admin-user" };

        _receiptRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<InventoryReceiptByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(InventoryReceiptStatus.Cancelled);
        result.Value.CancelledBy.Should().Be("admin-user");
        result.Value.CancelledAt.Should().NotBeNull();
        result.Value.CancellationReason.Should().Be("No longer needed");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullReason_ShouldCancelSuccessfully()
    {
        // Arrange
        var receipt = CreateDraftReceipt();
        var command = new CancelInventoryReceiptCommand(receipt.Id, null) { UserId = "admin-user" };

        _receiptRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<InventoryReceiptByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(InventoryReceiptStatus.Cancelled);
        result.Value.CancellationReason.Should().BeNull();
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenReceiptNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = new CancelInventoryReceiptCommand(Guid.NewGuid(), "Reason") { UserId = "admin-user" };

        _receiptRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<InventoryReceiptByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryReceipt?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-INVENTORY-003");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Business Rule Violations

    [Fact]
    public async Task Handle_WithConfirmedReceipt_ShouldReturnValidationError()
    {
        // Arrange
        var receipt = CreateDraftReceipt();
        receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", "SKU", 10, 25.00m);
        receipt.Confirm("user-1"); // Already confirmed
        var command = new CancelInventoryReceiptCommand(receipt.Id, "Changed mind") { UserId = "admin-user" };

        _receiptRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<InventoryReceiptByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-INVENTORY-005");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithAlreadyCancelledReceipt_ShouldReturnValidationError()
    {
        // Arrange
        var receipt = CreateDraftReceipt();
        receipt.Cancel("user-1"); // Already cancelled
        var command = new CancelInventoryReceiptCommand(receipt.Id, "Try again") { UserId = "admin-user" };

        _receiptRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<InventoryReceiptByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-INVENTORY-005");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToAllServices()
    {
        // Arrange
        var receipt = CreateDraftReceipt();
        var command = new CancelInventoryReceiptCommand(receipt.Id, "Reason") { UserId = "admin-user" };
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _receiptRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<InventoryReceiptByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _receiptRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<InventoryReceiptByIdForUpdateSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    #endregion
}
