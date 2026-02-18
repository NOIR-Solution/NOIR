using NOIR.Application.Features.Inventory.Commands.ConfirmInventoryReceipt;
using NOIR.Application.Features.Inventory.DTOs;
using NOIR.Application.Features.Inventory.Specifications;
using NOIR.Application.Features.Products.Specifications;
using NOIR.Domain.Entities.Inventory;
using NOIR.Domain.Entities.Product;

namespace NOIR.Application.UnitTests.Features.Inventory.Commands.ConfirmInventoryReceipt;

/// <summary>
/// Unit tests for ConfirmInventoryReceiptCommandHandler.
/// Tests receipt confirmation with stock adjustment scenarios.
/// </summary>
public class ConfirmInventoryReceiptCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<InventoryReceipt, Guid>> _receiptRepositoryMock;
    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IInventoryMovementLogger> _movementLoggerMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ConfirmInventoryReceiptCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public ConfirmInventoryReceiptCommandHandlerTests()
    {
        _receiptRepositoryMock = new Mock<IRepository<InventoryReceipt, Guid>>();
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _movementLoggerMock = new Mock<IInventoryMovementLogger>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new ConfirmInventoryReceiptCommandHandler(
            _receiptRepositoryMock.Object,
            _productRepositoryMock.Object,
            _movementLoggerMock.Object,
            _unitOfWorkMock.Object);
    }

    private static InventoryReceipt CreateDraftReceipt(
        InventoryReceiptType type = InventoryReceiptType.StockIn,
        string receiptNumber = "RCV-20260218-0001")
    {
        var receipt = InventoryReceipt.Create(receiptNumber, type, "Test notes", TestTenantId);
        return receipt;
    }

    private static InventoryReceipt CreateDraftReceiptWithItem(
        Guid? productId = null,
        Guid? variantId = null,
        InventoryReceiptType type = InventoryReceiptType.StockIn,
        int quantity = 10,
        decimal unitCost = 25.00m)
    {
        var receipt = CreateDraftReceipt(type);
        receipt.AddItem(
            variantId ?? Guid.NewGuid(),
            productId ?? Guid.NewGuid(),
            "Test Product",
            "Default",
            "SKU-001",
            quantity,
            unitCost);
        return receipt;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDraftStockInReceipt_ShouldConfirmAndAdjustStock()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("Test Product", "test-product", 99.99m, "VND", TestTenantId);
        var variant = product.AddVariant("Default", 50.00m, "SKU-001");
        variant.SetStock(100);

        var receipt = CreateDraftReceiptWithItem(productId: productId, variantId: variant.Id, quantity: 20);
        var command = new ConfirmInventoryReceiptCommand(receipt.Id) { UserId = "admin-user" };

        _receiptRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<InventoryReceiptByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _movementLoggerMock
            .Setup(x => x.LogMovementAsync(
                It.IsAny<ProductVariant>(),
                It.IsAny<InventoryMovementType>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(InventoryReceiptStatus.Confirmed);
        result.Value.ConfirmedBy.Should().Be("admin-user");
        result.Value.ConfirmedAt.Should().NotBeNull();

        // Stock should increase by 20 (100 + 20 = 120)
        variant.StockQuantity.Should().Be(120);

        _movementLoggerMock.Verify(
            x => x.LogMovementAsync(
                variant,
                InventoryMovementType.StockIn,
                100,
                20,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                "admin-user",
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDraftStockOutReceipt_ShouldConfirmAndDecreaseStock()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("Test Product", "test-product", 99.99m, "VND", TestTenantId);
        var variant = product.AddVariant("Default", 50.00m, "SKU-001");
        variant.SetStock(100);

        var receipt = CreateDraftReceiptWithItem(
            productId: productId,
            variantId: variant.Id,
            type: InventoryReceiptType.StockOut,
            quantity: 30);
        var command = new ConfirmInventoryReceiptCommand(receipt.Id) { UserId = "admin-user" };

        _receiptRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<InventoryReceiptByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _movementLoggerMock
            .Setup(x => x.LogMovementAsync(
                It.IsAny<ProductVariant>(),
                It.IsAny<InventoryMovementType>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(InventoryReceiptStatus.Confirmed);

        // Stock should decrease by 30 (100 - 30 = 70)
        variant.StockQuantity.Should().Be(70);

        _movementLoggerMock.Verify(
            x => x.LogMovementAsync(
                variant,
                InventoryMovementType.StockOut,
                100,
                -30,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                "admin-user",
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProductNotFoundForItem_ShouldSkipAndContinue()
    {
        // Arrange
        var receipt = CreateDraftReceiptWithItem();
        var command = new ConfirmInventoryReceiptCommand(receipt.Id) { UserId = "admin-user" };

        _receiptRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<InventoryReceiptByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(InventoryReceiptStatus.Confirmed);

        // Movement logger should NOT be called since product not found
        _movementLoggerMock.Verify(
            x => x.LogMovementAsync(
                It.IsAny<ProductVariant>(),
                It.IsAny<InventoryMovementType>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenReceiptNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = new ConfirmInventoryReceiptCommand(Guid.NewGuid()) { UserId = "admin-user" };

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
    public async Task Handle_WithAlreadyConfirmedReceipt_ShouldReturnValidationError()
    {
        // Arrange
        var receipt = CreateDraftReceiptWithItem();
        receipt.Confirm("user-1"); // Already confirmed
        var command = new ConfirmInventoryReceiptCommand(receipt.Id) { UserId = "admin-user" };

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
        result.Error.Code.Should().Be("NOIR-INVENTORY-004");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithCancelledReceipt_ShouldReturnValidationError()
    {
        // Arrange
        var receipt = CreateDraftReceipt();
        receipt.Cancel("user-1");
        var command = new ConfirmInventoryReceiptCommand(receipt.Id) { UserId = "admin-user" };

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
        result.Error.Code.Should().Be("NOIR-INVENTORY-004");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithStockOutExceedingAvailableStock_ShouldReturnValidationError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("Test Product", "test-product", 99.99m, "VND", TestTenantId);
        var variant = product.AddVariant("Default", 50.00m, "SKU-001");
        variant.SetStock(5); // Only 5 in stock

        var receipt = CreateDraftReceiptWithItem(
            productId: productId,
            variantId: variant.Id,
            type: InventoryReceiptType.StockOut,
            quantity: 50); // Want to take out 50
        var command = new ConfirmInventoryReceiptCommand(receipt.Id) { UserId = "admin-user" };

        _receiptRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<InventoryReceiptByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-INVENTORY-002");
        result.Error.Message.Should().Contain("Insufficient stock");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToAllServices()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("Test Product", "test-product", 99.99m, "VND", TestTenantId);
        var variant = product.AddVariant("Default", 50.00m, "SKU-001");
        variant.SetStock(100);

        var receipt = CreateDraftReceiptWithItem(productId: productId, variantId: variant.Id);
        var command = new ConfirmInventoryReceiptCommand(receipt.Id) { UserId = "admin-user" };
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _receiptRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<InventoryReceiptByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _movementLoggerMock
            .Setup(x => x.LogMovementAsync(
                It.IsAny<ProductVariant>(),
                It.IsAny<InventoryMovementType>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

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
