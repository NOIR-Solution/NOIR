using NOIR.Application.Features.Products.Commands.AddProductVariant;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products.Commands.AddProductVariant;

/// <summary>
/// Unit tests for AddProductVariantCommandHandler.
/// Tests adding variants to products with mocked dependencies.
/// </summary>
public class AddProductVariantCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IInventoryMovementLogger> _movementLoggerMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly AddProductVariantCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "test-user-id";

    public AddProductVariantCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _movementLoggerMock = new Mock<IInventoryMovementLogger>();

        _handler = new AddProductVariantCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _movementLoggerMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static AddProductVariantCommand CreateTestCommand(
        Guid? productId = null,
        string name = "Default Variant",
        decimal price = 99.99m,
        string? sku = "SKU-001",
        decimal? compareAtPrice = null,
        decimal? costPrice = null,
        int stockQuantity = 0,
        Dictionary<string, string>? options = null,
        int sortOrder = 0,
        string? userId = TestUserId)
    {
        return new AddProductVariantCommand(
            productId ?? Guid.NewGuid(),
            name,
            price,
            sku,
            compareAtPrice,
            costPrice,
            stockQuantity,
            options,
            sortOrder)
        {
            UserId = userId
        };
    }

    private static Product CreateTestProduct(
        string name = "Test Product",
        string slug = "test-product")
    {
        return Product.Create(name, slug, 99.99m, "VND", TestTenantId);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidData_ShouldAddVariant()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var command = CreateTestCommand(
            productId: productId,
            name: "Large",
            price: 129.99m,
            sku: "SKU-LARGE");

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
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("Large");
        result.Value.Price.ShouldBe(129.99m);
        result.Value.Sku.ShouldBe("SKU-LARGE");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCompareAtPrice_ShouldSetCompareAtPrice()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var command = CreateTestCommand(
            productId: productId,
            price: 79.99m,
            compareAtPrice: 99.99m);

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
        result.IsSuccess.ShouldBe(true);
        result.Value.CompareAtPrice.ShouldBe(99.99m);
        result.Value.OnSale.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_WithOptions_ShouldSetOptions()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var options = new Dictionary<string, string>
        {
            { "color", "Red" },
            { "size", "Large" }
        };
        var command = CreateTestCommand(
            productId: productId,
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
        result.IsSuccess.ShouldBe(true);
        result.Value.Options.ShouldNotBeNull();
        result.Value.Options.ShouldContainKey("color");
        result.Value.Options!["color"].ShouldBe("Red");
    }

    [Fact]
    public async Task Handle_WithStockQuantity_ShouldSetStockAndLogMovement()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var command = CreateTestCommand(
            productId: productId,
            stockQuantity: 50);

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
        result.IsSuccess.ShouldBe(true);
        result.Value.StockQuantity.ShouldBe(50);
        result.Value.InStock.ShouldBe(true);

        _movementLoggerMock.Verify(
            x => x.LogMovementAsync(
                It.IsAny<ProductVariant>(),
                InventoryMovementType.StockIn,
                0,
                50,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                TestUserId,
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithZeroStock_ShouldNotLogMovement()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var command = CreateTestCommand(
            productId: productId,
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
        result.IsSuccess.ShouldBe(true);
        result.Value.StockQuantity.ShouldBe(0);

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
    public async Task Handle_WithSortOrder_ShouldSetSortOrder()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var command = CreateTestCommand(
            productId: productId,
            sortOrder: 5);

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
        result.IsSuccess.ShouldBe(true);
        result.Value.SortOrder.ShouldBe(5);
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
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("NOIR-PRODUCT-021");
        result.Error.Message.ShouldContain("not found");

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
        var existingProduct = CreateTestProduct();
        var command = CreateTestCommand(productId: productId);
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
    public async Task Handle_WithNullSku_ShouldAddVariantWithoutSku()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var command = CreateTestCommand(
            productId: productId,
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
        result.IsSuccess.ShouldBe(true);
        result.Value.Sku.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_AddingMultipleVariants_ShouldMaintainCorrectCount()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        existingProduct.AddVariant("Small", 89.99m, "SKU-S");
        existingProduct.AddVariant("Medium", 99.99m, "SKU-M");

        var command = CreateTestCommand(
            productId: productId,
            name: "Large",
            sku: "SKU-L");

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
        result.IsSuccess.ShouldBe(true);
        existingProduct.Variants.Count().ShouldBe(3);
    }

    [Fact]
    public async Task Handle_WithoutCompareAtPrice_ShouldNotBeOnSale()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var command = CreateTestCommand(
            productId: productId,
            price: 99.99m,
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
        result.IsSuccess.ShouldBe(true);
        result.Value.CompareAtPrice.ShouldBeNull();
        result.Value.OnSale.ShouldBe(false);
    }

    #endregion
}
