using NOIR.Application.Features.ProductFilterIndex.EventHandlers;
using NOIR.Application.Features.ProductFilterIndex.Services;
using NOIR.Domain.Events.Product;

namespace NOIR.Application.UnitTests.Features.ProductFilterIndex;

/// <summary>
/// Unit tests for ProductFilterIndexSyncHandler.
/// Tests synchronization of ProductFilterIndex with product changes.
/// </summary>
public class ProductFilterIndexSyncHandlerTests
{
    #region Test Setup

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IRepository<ProductCategory, Guid>> _categoryRepositoryMock;
    private readonly Mock<IRepository<Brand, Guid>> _brandRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly AttributeJsonBuilder _attributeJsonBuilder;
    private readonly Mock<ILogger<ProductFilterIndexSyncHandler>> _loggerMock;
    private readonly ProductFilterIndexSyncHandler _handler;

    private const string TestTenantId = "test-tenant";

    public ProductFilterIndexSyncHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _categoryRepositoryMock = new Mock<IRepository<ProductCategory, Guid>>();
        _brandRepositoryMock = new Mock<IRepository<Brand, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _attributeJsonBuilder = new AttributeJsonBuilder();
        _loggerMock = new Mock<ILogger<ProductFilterIndexSyncHandler>>();

        _handler = new ProductFilterIndexSyncHandler(
            _dbContextMock.Object,
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _brandRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _attributeJsonBuilder,
            _loggerMock.Object);
    }

    private static Product CreateTestProduct(string name = "Test Product", string slug = "test-product")
    {
        return Product.Create(name, slug, 99.99m, "VND", TestTenantId);
    }

    private static ProductCategory CreateTestCategory(string name = "Test Category", string slug = "test-category")
    {
        return ProductCategory.Create(name, slug, null, TestTenantId);
    }

    private static Brand CreateTestBrand(string name = "Test Brand", string slug = "test-brand")
    {
        return Brand.Create(name, slug, TestTenantId);
    }

    private void SetupEmptyFilterIndexes()
    {
        var emptyList = new List<NOIR.Domain.Entities.Product.ProductFilterIndex>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductFilterIndexes).Returns(emptyList.Object);
    }

    private void SetupEmptyAssignments()
    {
        var emptyList = new List<ProductAttributeAssignment>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductAttributeAssignments).Returns(emptyList.Object);
    }

    #endregion

    #region ProductCreatedEvent Tests

    [Fact]
    public async Task Handle_ProductCreatedEvent_WithProductNotFound_DoesNotThrow()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var evt = new ProductCreatedEvent(productId, "New Product", "new-product");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Specification<Product>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        await _handler.Handle(evt, CancellationToken.None);

        // Handler should log warning but not throw
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductCreatedEvent_WithValidProduct_CreatesFilterIndex()
    {
        // Arrange
        var product = CreateTestProduct();
        var evt = new ProductCreatedEvent(product.Id, product.Name, product.Slug);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Specification<Product>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        SetupEmptyFilterIndexes();
        SetupEmptyAssignments();

        // Act
        await _handler.Handle(evt, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(x => x.ProductFilterIndexes.Add(
            It.Is<NOIR.Domain.Entities.Product.ProductFilterIndex>(fi =>
                fi.ProductId == product.Id &&
                fi.ProductName == product.Name)),
            Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductCreatedEvent_WithCategoryAndBrand_IncludesRelatedData()
    {
        // Arrange
        var category = CreateTestCategory("Electronics", "electronics");
        var brand = CreateTestBrand("Samsung", "samsung");
        var product = CreateTestProduct();

        // Use reflection to set CategoryId and BrandId
        typeof(Product).GetProperty("CategoryId")!.SetValue(product, category.Id);
        typeof(Product).GetProperty("BrandId")!.SetValue(product, brand.Id);

        var evt = new ProductCreatedEvent(product.Id, product.Name, product.Slug);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Specification<Product>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _brandRepositoryMock
            .Setup(x => x.GetByIdAsync(brand.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(brand);

        SetupEmptyFilterIndexes();
        SetupEmptyAssignments();

        // Act
        await _handler.Handle(evt, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(x => x.ProductFilterIndexes.Add(
            It.Is<NOIR.Domain.Entities.Product.ProductFilterIndex>(fi =>
                fi.ProductId == product.Id &&
                fi.CategoryId == category.Id &&
                fi.BrandId == brand.Id)),
            Times.Once);
    }

    #endregion

    #region ProductUpdatedEvent Tests

    [Fact]
    public async Task Handle_ProductUpdatedEvent_WithProductNotFound_DoesNotThrow()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var evt = new ProductUpdatedEvent(productId, "Updated Product");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Specification<Product>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        await _handler.Handle(evt, CancellationToken.None);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductUpdatedEvent_WithExistingIndex_UpdatesIndex()
    {
        // Arrange
        var product = CreateTestProduct("Updated Product", "updated-product");
        var evt = new ProductUpdatedEvent(product.Id, product.Name);

        var existingIndex = NOIR.Domain.Entities.Product.ProductFilterIndex.Create(
            product.Id, "Old Name", "old-slug", ProductStatus.Draft, 50m, "VND", TestTenantId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Specification<Product>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var filterIndexList = new List<NOIR.Domain.Entities.Product.ProductFilterIndex> { existingIndex }
            .BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductFilterIndexes).Returns(filterIndexList.Object);

        SetupEmptyAssignments();

        // Act
        await _handler.Handle(evt, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        // Note: The actual update happens via the entity method, which modifies the tracked entity
    }

    [Fact]
    public async Task Handle_ProductUpdatedEvent_WithNoIndex_CreatesNewIndex()
    {
        // Arrange
        var product = CreateTestProduct("New Product", "new-product");
        var evt = new ProductUpdatedEvent(product.Id, product.Name);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Specification<Product>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        SetupEmptyFilterIndexes();
        SetupEmptyAssignments();

        // Act
        await _handler.Handle(evt, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(x => x.ProductFilterIndexes.Add(
            It.IsAny<NOIR.Domain.Entities.Product.ProductFilterIndex>()),
            Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ProductPublishedEvent Tests

    [Fact]
    public async Task Handle_ProductPublishedEvent_SyncsFilterIndex()
    {
        // Arrange
        var product = CreateTestProduct();
        product.Publish(); // Set status to Active
        var evt = new ProductPublishedEvent(product.Id, product.Name);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Specification<Product>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        SetupEmptyFilterIndexes();
        SetupEmptyAssignments();

        // Act
        await _handler.Handle(evt, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(x => x.ProductFilterIndexes.Add(
            It.Is<NOIR.Domain.Entities.Product.ProductFilterIndex>(fi =>
                fi.ProductId == product.Id &&
                fi.Status == ProductStatus.Active)),
            Times.Once);
    }

    #endregion

    #region ProductArchivedEvent Tests

    [Fact]
    public async Task Handle_ProductArchivedEvent_SyncsFilterIndex()
    {
        // Arrange
        var product = CreateTestProduct();
        product.Archive(); // Set status to Archived
        var evt = new ProductArchivedEvent(product.Id);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Specification<Product>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        SetupEmptyFilterIndexes();
        SetupEmptyAssignments();

        // Act
        await _handler.Handle(evt, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(x => x.ProductFilterIndexes.Add(
            It.Is<NOIR.Domain.Entities.Product.ProductFilterIndex>(fi =>
                fi.ProductId == product.Id &&
                fi.Status == ProductStatus.Archived)),
            Times.Once);
    }

    #endregion

    #region ProductStockChangedEvent Tests

    [Fact]
    public async Task Handle_ProductStockChangedEvent_UpdatesStockInfo()
    {
        // Arrange
        var product = CreateTestProduct();
        var variantId = Guid.NewGuid();
        var evt = new ProductStockChangedEvent(variantId, product.Id, 0, 10, InventoryMovementType.StockIn);

        var existingIndex = NOIR.Domain.Entities.Product.ProductFilterIndex.Create(
            product.Id, product.Name, product.Slug, ProductStatus.Active, 99.99m, "VND", TestTenantId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Specification<Product>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var filterIndexList = new List<NOIR.Domain.Entities.Product.ProductFilterIndex> { existingIndex }
            .BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductFilterIndexes).Returns(filterIndexList.Object);

        // Act
        await _handler.Handle(evt, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductStockChangedEvent_WithNoIndex_CreatesIndex()
    {
        // Arrange
        var product = CreateTestProduct();
        var variantId = Guid.NewGuid();
        var evt = new ProductStockChangedEvent(variantId, product.Id, 0, 10, InventoryMovementType.StockIn);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Specification<Product>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        SetupEmptyFilterIndexes();
        SetupEmptyAssignments();

        // Act
        await _handler.Handle(evt, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(x => x.ProductFilterIndexes.Add(
            It.IsAny<NOIR.Domain.Entities.Product.ProductFilterIndex>()),
            Times.Once);
    }

    #endregion

    #region ProductAttributeAssignmentChangedEvent Tests

    [Fact]
    public async Task Handle_ProductAttributeAssignmentChangedEvent_UpdatesAttributesJson()
    {
        // Arrange
        var product = CreateTestProduct();
        var evt = new ProductAttributeAssignmentChangedEvent(product.Id, null);

        var existingIndex = NOIR.Domain.Entities.Product.ProductFilterIndex.Create(
            product.Id, product.Name, product.Slug, ProductStatus.Active, 99.99m, "VND", TestTenantId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Specification<Product>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var filterIndexList = new List<NOIR.Domain.Entities.Product.ProductFilterIndex> { existingIndex }
            .BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductFilterIndexes).Returns(filterIndexList.Object);

        SetupEmptyAssignments();

        // Act
        await _handler.Handle(evt, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductAttributeAssignmentChangedEvent_WithNoIndex_CreatesIndex()
    {
        // Arrange
        var product = CreateTestProduct();
        var evt = new ProductAttributeAssignmentChangedEvent(product.Id, null);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Specification<Product>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        SetupEmptyFilterIndexes();
        SetupEmptyAssignments();

        // Act
        await _handler.Handle(evt, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(x => x.ProductFilterIndexes.Add(
            It.IsAny<NOIR.Domain.Entities.Product.ProductFilterIndex>()),
            Times.Once);
    }

    #endregion
}
