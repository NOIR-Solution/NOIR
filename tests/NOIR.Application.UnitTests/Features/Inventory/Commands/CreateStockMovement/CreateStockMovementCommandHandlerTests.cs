using NOIR.Application.Features.Inventory.Commands.CreateStockMovement;
using NOIR.Application.Features.Inventory.DTOs;
using NOIR.Application.Features.Products.Specifications;
using NOIR.Domain.Entities.Product;

namespace NOIR.Application.UnitTests.Features.Inventory.Commands.CreateStockMovement;

/// <summary>
/// Unit tests for CreateStockMovementCommandHandler.
/// Tests stock movement creation for StockIn, StockOut, and Adjustment types.
/// </summary>
public class CreateStockMovementCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IInventoryMovementLogger> _movementLoggerMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateStockMovementCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public CreateStockMovementCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _movementLoggerMock = new Mock<IInventoryMovementLogger>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new CreateStockMovementCommandHandler(
            _productRepositoryMock.Object,
            _movementLoggerMock.Object,
            _unitOfWorkMock.Object);
    }

    private static Product CreateTestProduct(
        string name = "Test Product",
        string slug = "test-product",
        int initialStock = 100)
    {
        var product = Product.Create(name, slug, 99.99m, "VND", TestTenantId);
        var variant = product.AddVariant("Default", 50.00m, "SKU-001");
        variant.SetStock(initialStock);
        return product;
    }

    private static CreateStockMovementCommand CreateTestCommand(
        Guid? productId = null,
        Guid? variantId = null,
        InventoryMovementType movementType = InventoryMovementType.StockIn,
        int quantity = 10,
        string? reference = null,
        string? notes = null)
    {
        return new CreateStockMovementCommand(
            productId ?? Guid.NewGuid(),
            variantId ?? Guid.NewGuid(),
            movementType,
            quantity,
            reference,
            notes);
    }

    #endregion

    #region StockIn Scenarios

    [Fact]
    public async Task Handle_WithStockIn_ShouldIncreaseStockSuccessfully()
    {
        // Arrange
        var product = CreateTestProduct(initialStock: 50);
        var variant = product.Variants.First();
        var command = CreateTestCommand(
            productId: product.Id,
            variantId: variant.Id,
            movementType: InventoryMovementType.StockIn,
            quantity: 20,
            reference: "PO-001",
            notes: "Supplier delivery");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

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

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MovementType.Should().Be(InventoryMovementType.StockIn);
        result.Value.QuantityBefore.Should().Be(50);
        result.Value.QuantityMoved.Should().Be(20);
        result.Value.QuantityAfter.Should().Be(70);
        result.Value.Reference.Should().Be("PO-001");
        result.Value.Notes.Should().Be("Supplier delivery");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _movementLoggerMock.Verify(
            x => x.LogMovementAsync(
                variant,
                InventoryMovementType.StockIn,
                50,
                20,
                "PO-001",
                "Supplier delivery",
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region StockOut Scenarios

    [Fact]
    public async Task Handle_WithStockOut_ShouldDecreaseStockSuccessfully()
    {
        // Arrange
        var product = CreateTestProduct(initialStock: 100);
        var variant = product.Variants.First();
        var command = CreateTestCommand(
            productId: product.Id,
            variantId: variant.Id,
            movementType: InventoryMovementType.StockOut,
            quantity: 30);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

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

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MovementType.Should().Be(InventoryMovementType.StockOut);
        result.Value.QuantityBefore.Should().Be(100);
        result.Value.QuantityMoved.Should().Be(-30);
        result.Value.QuantityAfter.Should().Be(70);

        _movementLoggerMock.Verify(
            x => x.LogMovementAsync(
                variant,
                InventoryMovementType.StockOut,
                100,
                -30,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithStockOutExceedingAvailable_ShouldReturnValidationError()
    {
        // Arrange
        var product = CreateTestProduct(initialStock: 10);
        var variant = product.Variants.First();
        var command = CreateTestCommand(
            productId: product.Id,
            variantId: variant.Id,
            movementType: InventoryMovementType.StockOut,
            quantity: 50); // More than available

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

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Adjustment Scenarios

    [Fact]
    public async Task Handle_WithPositiveAdjustment_ShouldIncreaseStock()
    {
        // Arrange
        var product = CreateTestProduct(initialStock: 50);
        var variant = product.Variants.First();
        var command = CreateTestCommand(
            productId: product.Id,
            variantId: variant.Id,
            movementType: InventoryMovementType.Adjustment,
            quantity: 15,
            notes: "Physical count correction");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

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

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MovementType.Should().Be(InventoryMovementType.Adjustment);
        result.Value.QuantityBefore.Should().Be(50);
        result.Value.QuantityMoved.Should().Be(15);
        result.Value.QuantityAfter.Should().Be(65);
    }

    [Fact]
    public async Task Handle_WithNegativeAdjustmentExceedingStock_ShouldReturnValidationError()
    {
        // Arrange
        var product = CreateTestProduct(initialStock: 10);
        var variant = product.Variants.First();
        var command = CreateTestCommand(
            productId: product.Id,
            variantId: variant.Id,
            movementType: InventoryMovementType.Adjustment,
            quantity: -20); // Would make stock negative

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

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Invalid Movement Types

    [Theory]
    [InlineData(InventoryMovementType.Return)]
    [InlineData(InventoryMovementType.Reservation)]
    [InlineData(InventoryMovementType.ReservationRelease)]
    [InlineData(InventoryMovementType.Damaged)]
    [InlineData(InventoryMovementType.Expired)]
    public async Task Handle_WithDisallowedMovementType_ShouldReturnValidationError(
        InventoryMovementType movementType)
    {
        // Arrange
        var command = CreateTestCommand(movementType: movementType);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-INVENTORY-001");
        result.Error.Message.Should().Contain("Only StockIn, StockOut, and Adjustment are allowed");

        _productRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = CreateTestCommand(movementType: InventoryMovementType.StockIn);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-PRODUCT-022");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenVariantNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var product = CreateTestProduct();
        var nonExistentVariantId = Guid.NewGuid();
        var command = CreateTestCommand(
            productId: product.Id,
            variantId: nonExistentVariantId,
            movementType: InventoryMovementType.StockIn);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-PRODUCT-023");

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
        var product = CreateTestProduct(initialStock: 50);
        var variant = product.Variants.First();
        var command = CreateTestCommand(
            productId: product.Id,
            variantId: variant.Id,
            movementType: InventoryMovementType.StockIn,
            quantity: 10);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

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

        // Act
        await _handler.Handle(command, token);

        // Assert
        _productRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductByIdForUpdateSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullReferenceAndNotes_ShouldSucceed()
    {
        // Arrange
        var product = CreateTestProduct(initialStock: 50);
        var variant = product.Variants.First();
        var command = CreateTestCommand(
            productId: product.Id,
            variantId: variant.Id,
            movementType: InventoryMovementType.StockIn,
            quantity: 5,
            reference: null,
            notes: null);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

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

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Reference.Should().BeNull();
        result.Value.Notes.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldPassUserIdToMovementLogger()
    {
        // Arrange
        var product = CreateTestProduct(initialStock: 50);
        var variant = product.Variants.First();
        var command = CreateTestCommand(
            productId: product.Id,
            variantId: variant.Id,
            movementType: InventoryMovementType.StockIn,
            quantity: 10) with { UserId = "admin-user-123" };

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

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

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _movementLoggerMock.Verify(
            x => x.LogMovementAsync(
                It.IsAny<ProductVariant>(),
                InventoryMovementType.StockIn,
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                "admin-user-123",
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
