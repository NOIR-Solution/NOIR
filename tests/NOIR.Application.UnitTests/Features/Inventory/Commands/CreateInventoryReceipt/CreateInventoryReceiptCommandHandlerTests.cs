using NOIR.Application.Features.Inventory.Commands.CreateInventoryReceipt;
using NOIR.Application.Features.Inventory.DTOs;
using NOIR.Application.Features.Inventory.Specifications;
using NOIR.Application.Features.Products.Specifications;
using NOIR.Domain.Entities.Inventory;
using NOIR.Domain.Entities.Product;

namespace NOIR.Application.UnitTests.Features.Inventory.Commands.CreateInventoryReceipt;

/// <summary>
/// Unit tests for CreateInventoryReceiptCommandHandler.
/// Tests receipt creation, numbering, item addition, and product/variant validation.
/// </summary>
public class CreateInventoryReceiptCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<InventoryReceipt, Guid>> _receiptRepositoryMock;
    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateInventoryReceiptCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public CreateInventoryReceiptCommandHandlerTests()
    {
        _receiptRepositoryMock = new Mock<IRepository<InventoryReceipt, Guid>>();
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new CreateInventoryReceiptCommandHandler(
            _receiptRepositoryMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    /// <summary>
    /// Creates test items backed by real products with variants so validation passes.
    /// Returns both the items and the products they reference.
    /// </summary>
    private static (List<CreateInventoryReceiptItemDto> Items, List<Product> Products) CreateTestItemsWithProducts(int count = 1)
    {
        var items = new List<CreateInventoryReceiptItemDto>();
        var products = new List<Product>();

        for (int i = 0; i < count; i++)
        {
            var product = Product.Create($"Product {i + 1}", $"product-{i + 1}", 100m + i, "VND", TestTenantId);
            var variant = product.AddVariant($"Variant {i + 1}", 50m + i, $"SKU-{i + 1:D3}");

            items.Add(new CreateInventoryReceiptItemDto(
                variant.Id,
                product.Id,
                product.Name,
                variant.Name,
                variant.Sku,
                10 + i,
                25.00m + i));

            products.Add(product);
        }

        return (items, products);
    }

    private void SetupProductRepositoryMock(List<Product> products)
    {
        foreach (var product in products)
        {
            _productRepositoryMock
                .Setup(x => x.FirstOrDefaultAsync(
                    It.Is<ProductByIdSpec>(s => true),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProductByIdSpec spec, CancellationToken _) =>
                {
                    // Return the matching product based on the spec
                    return products.FirstOrDefault(p =>
                    {
                        // The spec filters by product ID - match by checking all products
                        return true;
                    });
                });
        }

        // More precise setup: return product matching the queried ID
        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductByIdSpec _, CancellationToken __) =>
            {
                // Since we can't easily extract the ID from the spec,
                // return the first product for single-item tests,
                // or rely on ordering for multi-item tests.
                // For proper testing, we use the sequential approach below.
                return products.FirstOrDefault();
            });
    }

    private void SetupProductRepositoryMockSequential(List<Product> products)
    {
        var sequence = _productRepositoryMock.SetupSequence(x =>
            x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdSpec>(),
                It.IsAny<CancellationToken>()));

        foreach (var product in products)
        {
            sequence.ReturnsAsync(product);
        }
    }

    private void SetupCommonMocks()
    {
        _receiptRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestReceiptNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryReceipt?)null);

        _receiptRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<InventoryReceipt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryReceipt r, CancellationToken _) => r);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithStockInType_ShouldCreateReceiptWithRCVPrefix()
    {
        // Arrange
        var (items, products) = CreateTestItemsWithProducts();
        var command = new CreateInventoryReceiptCommand(
            InventoryReceiptType.StockIn,
            "Supplier delivery",
            items);

        SetupProductRepositoryMockSequential(products);
        SetupCommonMocks();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ReceiptNumber.Should().StartWith("RCV-");
        result.Value.ReceiptNumber.Should().EndWith("-0001");
        result.Value.Type.Should().Be(InventoryReceiptType.StockIn);
        result.Value.Status.Should().Be(InventoryReceiptStatus.Draft);
        result.Value.Notes.Should().Be("Supplier delivery");

        _receiptRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<InventoryReceipt>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithStockOutType_ShouldCreateReceiptWithSHPPrefix()
    {
        // Arrange
        var (items, products) = CreateTestItemsWithProducts();
        var command = new CreateInventoryReceiptCommand(
            InventoryReceiptType.StockOut,
            null,
            items);

        SetupProductRepositoryMockSequential(products);
        SetupCommonMocks();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ReceiptNumber.Should().StartWith("SHP-");
        result.Value.Type.Should().Be(InventoryReceiptType.StockOut);
    }

    [Fact]
    public async Task Handle_WithMultipleItems_ShouldAddAllItemsToReceipt()
    {
        // Arrange
        var (items, products) = CreateTestItemsWithProducts(3);
        var command = new CreateInventoryReceiptCommand(
            InventoryReceiptType.StockIn,
            "Bulk delivery",
            items);

        SetupProductRepositoryMockSequential(products);
        SetupCommonMocks();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithExistingReceiptToday_ShouldIncrementSequence()
    {
        // Arrange
        var (items, products) = CreateTestItemsWithProducts();
        var command = new CreateInventoryReceiptCommand(
            InventoryReceiptType.StockIn,
            null,
            items);

        var dateStr = DateTimeOffset.UtcNow.ToString("yyyyMMdd");
        var existingReceipt = InventoryReceipt.Create($"RCV-{dateStr}-0005", InventoryReceiptType.StockIn);

        SetupProductRepositoryMockSequential(products);

        _receiptRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestReceiptNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReceipt);

        _receiptRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<InventoryReceipt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryReceipt r, CancellationToken _) => r);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ReceiptNumber.Should().EndWith("-0006");
    }

    [Fact]
    public async Task Handle_WithNullNotes_ShouldSucceed()
    {
        // Arrange
        var (items, products) = CreateTestItemsWithProducts();
        var command = new CreateInventoryReceiptCommand(
            InventoryReceiptType.StockIn,
            null,
            items);

        SetupProductRepositoryMockSequential(products);
        SetupCommonMocks();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Notes.Should().BeNull();
    }

    #endregion

    #region Product/Variant Validation

    [Fact]
    public async Task Handle_WithNonExistentProduct_ShouldReturnFailure()
    {
        // Arrange
        var items = new List<CreateInventoryReceiptItemDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Ghost Product", "Default", "SKU-001", 10, 25.00m)
        };
        var command = new CreateInventoryReceiptCommand(
            InventoryReceiptType.StockIn,
            null,
            items);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOIR-INVENTORY-005");

        _receiptRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<InventoryReceipt>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentVariant_ShouldReturnFailure()
    {
        // Arrange
        var product = Product.Create("Test Product", "test-product", 100m, "VND", TestTenantId);
        product.AddVariant("Default", 50m, "SKU-001");

        // Create item referencing the correct product but a non-existent variant
        var items = new List<CreateInventoryReceiptItemDto>
        {
            new(Guid.NewGuid(), product.Id, "Test Product", "Non-existent Variant", "SKU-999", 10, 25.00m)
        };
        var command = new CreateInventoryReceiptCommand(
            InventoryReceiptType.StockIn,
            null,
            items);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOIR-INVENTORY-006");

        _receiptRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<InventoryReceipt>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToAllServices()
    {
        // Arrange
        var (items, products) = CreateTestItemsWithProducts();
        var command = new CreateInventoryReceiptCommand(
            InventoryReceiptType.StockIn,
            null,
            items);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        SetupProductRepositoryMockSequential(products);
        SetupCommonMocks();

        // Act
        await _handler.Handle(command, token);

        // Assert
        _productRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductByIdSpec>(), token),
            Times.Once);
        _receiptRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<LatestReceiptNumberTodaySpec>(), token),
            Times.Once);
        _receiptRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<InventoryReceipt>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    #endregion
}
