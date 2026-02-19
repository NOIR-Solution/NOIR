using NOIR.Application.Features.Products.Commands.UpdateProductVariant;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products.Commands.UpdateProductVariant;

/// <summary>
/// Unit tests for UpdateProductVariantCommandHandler.
/// Tests updating product variants with mocked dependencies.
/// </summary>
public class UpdateProductVariantCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IInventoryMovementLogger> _movementLoggerMock;
    private readonly UpdateProductVariantCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public UpdateProductVariantCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _movementLoggerMock = new Mock<IInventoryMovementLogger>();

        _handler = new UpdateProductVariantCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _movementLoggerMock.Object);
    }

    private static UpdateProductVariantCommand CreateTestCommand(
        Guid? productId = null,
        Guid? variantId = null,
        string name = "Updated Variant",
        decimal price = 149.99m,
        string? sku = null,
        decimal? compareAtPrice = null,
        decimal? costPrice = null,
        int stockQuantity = 10,
        Dictionary<string, string>? options = null,
        int sortOrder = 0)
    {
        return new UpdateProductVariantCommand(
            productId ?? Guid.NewGuid(),
            variantId ?? Guid.NewGuid(),
            name,
            price,
            sku,
            compareAtPrice,
            costPrice,
            stockQuantity,
            options,
            sortOrder);
    }

    private static Product CreateTestProduct(
        string name = "Test Product",
        string slug = "test-product")
    {
        return Product.Create(name, slug, 99.99m, "VND", TestTenantId);
    }

    private static Product CreateTestProductWithVariants()
    {
        var product = CreateTestProduct();
        product.AddVariant("Small", 19.99m, "SKU-S");
        product.AddVariant("Large", 24.99m, "SKU-L");
        return product;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidData_ShouldUpdateVariant()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithVariants();
        var variantId = existingProduct.Variants.First().Id;

        var command = CreateTestCommand(
            productId: productId,
            variantId: variantId,
            name: "Medium",
            price: 22.99m,
            sku: "SKU-M",
            compareAtPrice: 29.99m,
            stockQuantity: 50,
            sortOrder: 1);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var updatedVariant = existingProduct.Variants.First(v => v.Id == variantId);
        updatedVariant.Name.Should().Be("Medium");
        updatedVariant.Price.Should().Be(22.99m);
        updatedVariant.Sku.Should().Be("SKU-M");
        updatedVariant.CompareAtPrice.Should().Be(29.99m);
        updatedVariant.StockQuantity.Should().Be(50);
        updatedVariant.SortOrder.Should().Be(1);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithStockChange_ShouldLogInventoryMovement()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithVariants();
        var variant = existingProduct.Variants.First();
        variant.SetStock(10); // Initial stock

        var command = CreateTestCommand(
            productId: productId,
            variantId: variant.Id,
            stockQuantity: 25); // Increase by 15

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _movementLoggerMock.Verify(
            x => x.LogMovementAsync(
                It.IsAny<ProductVariant>(),
                InventoryMovementType.Adjustment,
                10, // quantityBefore
                15, // quantityMoved (25 - 10)
                It.IsAny<string?>(),
                It.Is<string?>(s => s != null && s.Contains("Manual stock adjustment")),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoStockChange_ShouldNotLogInventoryMovement()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithVariants();
        var variant = existingProduct.Variants.First();
        variant.SetStock(10);

        var command = CreateTestCommand(
            productId: productId,
            variantId: variant.Id,
            stockQuantity: 10); // Same stock

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
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

    [Fact]
    public async Task Handle_WithOptions_ShouldUpdateOptions()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithVariants();
        var variantId = existingProduct.Variants.First().Id;

        var options = new Dictionary<string, string>
        {
            { "color", "red" },
            { "size", "medium" }
        };

        var command = CreateTestCommand(
            productId: productId,
            variantId: variantId,
            options: options);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updatedVariant = existingProduct.Variants.First(v => v.Id == variantId);
        var variantOptions = updatedVariant.GetOptions();
        variantOptions.Should().ContainKey("color");
        variantOptions["color"].Should().Be("red");
    }

    [Fact]
    public async Task Handle_WithNullCompareAtPrice_ShouldClearCompareAtPrice()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithVariants();
        var variant = existingProduct.Variants.First();
        variant.SetCompareAtPrice(199.99m);

        var command = CreateTestCommand(
            productId: productId,
            variantId: variant.Id,
            compareAtPrice: null);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingProduct.Variants.First(v => v.Id == variant.Id).CompareAtPrice.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnProductDto()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithVariants();
        var variantId = existingProduct.Variants.First().Id;

        var command = CreateTestCommand(productId: productId, variantId: variantId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(existingProduct.Id);
        result.Value.Name.Should().Be(existingProduct.Name);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = CreateTestCommand();

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
        result.Error.Message.Should().Contain("Product");
        result.Error.Message.Should().Contain("not found");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenVariantNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithVariants();
        var nonExistentVariantId = Guid.NewGuid();

        var command = CreateTestCommand(productId: productId, variantId: nonExistentVariantId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-PRODUCT-023");
        result.Error.Message.Should().Contain("Variant");
        result.Error.Message.Should().Contain("not found");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithVariants();
        var variantId = existingProduct.Variants.First().Id;
        var command = CreateTestCommand(productId: productId, variantId: variantId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

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
    public async Task Handle_WithStockDecrease_ShouldLogNegativeMovement()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithVariants();
        var variant = existingProduct.Variants.First();
        variant.SetStock(50);

        var command = CreateTestCommand(
            productId: productId,
            variantId: variant.Id,
            stockQuantity: 30); // Decrease by 20

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _movementLoggerMock.Verify(
            x => x.LogMovementAsync(
                It.IsAny<ProductVariant>(),
                InventoryMovementType.Adjustment,
                50, // quantityBefore
                -20, // quantityMoved (30 - 50)
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithZeroStock_ShouldUpdateToZeroStock()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithVariants();
        var variant = existingProduct.Variants.First();
        variant.SetStock(10);

        var command = CreateTestCommand(
            productId: productId,
            variantId: variant.Id,
            stockQuantity: 0);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingProduct.Variants.First(v => v.Id == variant.Id).StockQuantity.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithNullOptions_ShouldNotUpdateOptions()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithVariants();
        var variant = existingProduct.Variants.First();
        variant.UpdateOptions(new Dictionary<string, string> { { "color", "blue" } });

        var command = CreateTestCommand(
            productId: productId,
            variantId: variant.Id,
            options: null);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Options should not be cleared when null is passed
    }

    [Fact]
    public async Task Handle_UpdatingSecondVariant_ShouldNotAffectFirstVariant()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithVariants();
        var firstVariantId = existingProduct.Variants.First().Id;
        var secondVariantId = existingProduct.Variants.ToList()[1].Id;

        var command = CreateTestCommand(
            productId: productId,
            variantId: secondVariantId,
            name: "Extra Large",
            price: 29.99m);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var firstVariant = existingProduct.Variants.First(v => v.Id == firstVariantId);
        firstVariant.Name.Should().Be("Small");
        firstVariant.Price.Should().Be(19.99m);
    }

    [Fact]
    public async Task Handle_WithNullSku_ShouldClearSku()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithVariants();
        var variantId = existingProduct.Variants.First().Id;

        var command = CreateTestCommand(
            productId: productId,
            variantId: variantId,
            sku: null);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingProduct.Variants.First(v => v.Id == variantId).Sku.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithUserIdInCommand_ShouldPassToMovementLogger()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var userId = "user-123";
        var existingProduct = CreateTestProductWithVariants();
        var variant = existingProduct.Variants.First();
        variant.SetStock(10);

        var command = CreateTestCommand(
            productId: productId,
            variantId: variant.Id,
            stockQuantity: 20) with { UserId = userId };

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _movementLoggerMock.Verify(
            x => x.LogMovementAsync(
                It.IsAny<ProductVariant>(),
                It.IsAny<InventoryMovementType>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                userId,
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
