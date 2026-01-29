using NOIR.Application.Features.Products.Commands.DuplicateProduct;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products;

/// <summary>
/// Unit tests for DuplicateProductCommandHandler.
/// Tests product duplication scenarios with mocked dependencies.
/// </summary>
public class DuplicateProductCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IRepository<ProductCategory, Guid>> _categoryRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly DuplicateProductCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public DuplicateProductCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _categoryRepositoryMock = new Mock<IRepository<ProductCategory, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DuplicateProductCommandHandler(
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static DuplicateProductCommand CreateTestCommand(
        Guid productId,
        bool copyVariants = false,
        bool copyImages = false,
        bool copyOptions = false)
    {
        return new DuplicateProductCommand(productId, copyVariants, copyImages, copyOptions);
    }

    private static Product CreateTestProduct(
        Guid? id = null,
        string name = "Test Product",
        string slug = "test-product",
        ProductStatus status = ProductStatus.Draft)
    {
        var product = Product.Create(name, slug, 99.99m, "VND", TestTenantId);

        // Use reflection to set the Id for testing
        if (id.HasValue)
        {
            typeof(Product).GetProperty("Id")!.SetValue(product, id.Value);
        }

        // Set basic info
        product.UpdateBasicInfo(name, slug, "Short description", "Full description", "<p>Full description</p>");
        product.UpdateIdentification("SKU-001", "1234567890123");
        product.UpdateSeo("SEO Title", "SEO Description");

        return product;
    }

    private static ProductCategory CreateTestCategory(Guid? id = null, string name = "Test Category")
    {
        var category = ProductCategory.Create(name, name.ToLowerInvariant().Replace(" ", "-"), null, TestTenantId);
        if (id.HasValue)
        {
            typeof(ProductCategory).GetProperty("Id")!.SetValue(category, id.Value);
        }
        return category;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidProduct_ShouldCreateDuplicate()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var originalProduct = CreateTestProduct(productId, "Original Product", "original-product");

        var command = CreateTestCommand(productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdWithCollectionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalProduct);

        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Contain("(Copy)");
        result.Value.Slug.Should().Contain("-copy-");
        result.Value.Status.Should().Be(ProductStatus.Draft); // Duplicates always start as Draft

        _productRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCategory_ShouldCopyCategoryAssignment()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var originalProduct = CreateTestProduct(productId, "Original Product", "original-product");
        originalProduct.SetCategory(categoryId);

        var category = CreateTestCategory(categoryId, "Test Category");

        var command = CreateTestCommand(productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdWithCollectionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalProduct);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryId.Should().Be(categoryId);
        result.Value.CategoryName.Should().Be("Test Category");
    }

    [Fact]
    public async Task Handle_WithCopyImages_ShouldCopyImages()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var originalProduct = CreateTestProduct(productId, "Original Product", "original-product");
        originalProduct.AddImage("https://example.com/image1.jpg", "Image 1", true);
        originalProduct.AddImage("https://example.com/image2.jpg", "Image 2", false);

        var command = CreateTestCommand(productId, copyImages: true);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdWithCollectionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalProduct);

        Product? capturedProduct = null;
        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedProduct.Should().NotBeNull();
        capturedProduct!.Images.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithCopyOptions_ShouldCopyOptions()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var originalProduct = CreateTestProduct(productId, "Original Product", "original-product");
        var colorOption = originalProduct.AddOption("Color", "Color");
        colorOption.AddValue("Red", "Red");
        colorOption.AddValue("Blue", "Blue");

        var sizeOption = originalProduct.AddOption("Size", "Size");
        sizeOption.AddValue("S", "Small");
        sizeOption.AddValue("M", "Medium");

        var command = CreateTestCommand(productId, copyOptions: true);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdWithCollectionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalProduct);

        Product? capturedProduct = null;
        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedProduct.Should().NotBeNull();
        capturedProduct!.Options.Should().HaveCount(2);
        capturedProduct.Options.SelectMany(o => o.Values).Should().HaveCount(4);
    }

    [Fact]
    public async Task Handle_WithCopyVariants_ShouldCopyVariants()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var originalProduct = CreateTestProduct(productId, "Original Product", "original-product");
        originalProduct.AddVariant("Red - Small", 99.99m, "SKU-RED-S", new Dictionary<string, string> { ["Color"] = "Red", ["Size"] = "S" });
        originalProduct.AddVariant("Blue - Medium", 109.99m, "SKU-BLUE-M", new Dictionary<string, string> { ["Color"] = "Blue", ["Size"] = "M" });

        var command = CreateTestCommand(productId, copyVariants: true);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdWithCollectionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalProduct);

        Product? capturedProduct = null;
        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedProduct.Should().NotBeNull();
        capturedProduct!.Variants.Should().HaveCount(2);
        // SKUs should have -COPY suffix
        capturedProduct.Variants.Should().AllSatisfy(v => v.Sku.Should().EndWith("-COPY"));
    }

    [Fact]
    public async Task Handle_ShouldModifySkuWithCopySuffix()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var originalProduct = CreateTestProduct(productId, "Original Product", "original-product");

        var command = CreateTestCommand(productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdWithCollectionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalProduct);

        Product? capturedProduct = null;
        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedProduct.Should().NotBeNull();
        capturedProduct!.Sku.Should().Be("SKU-001-COPY");
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WithNonExistentProduct_ShouldReturnNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = CreateTestCommand(productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdWithCollectionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-PRODUCT-060");

        _productRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);

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
        var originalProduct = CreateTestProduct(productId, "Original Product", "original-product");
        var command = CreateTestCommand(productId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdWithCollectionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalProduct);

        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _productRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductByIdWithCollectionsSpec>(), token),
            Times.Once);
        _productRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Product>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullOptionalFields_ShouldHandleGracefully()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var originalProduct = Product.Create("Simple Product", "simple-product", 50m, "VND", TestTenantId);
        typeof(Product).GetProperty("Id")!.SetValue(originalProduct, productId);
        // Don't set SKU, brand, description - leave them null

        var command = CreateTestCommand(productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdWithCollectionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalProduct);

        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Sku.Should().BeNull(); // Null SKU should remain null (not "null-COPY")
    }

    [Fact]
    public async Task Handle_ShouldGenerateUniqueSlugWithTimestamp()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var originalProduct = CreateTestProduct(productId, "Original Product", "original-product");

        var command = CreateTestCommand(productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdWithCollectionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalProduct);

        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().StartWith("original-product-copy-");
        // Slug should contain hex timestamp
        var slugSuffix = result.Value.Slug.Replace("original-product-copy-", "");
        slugSuffix.Should().MatchRegex("^[0-9a-f]+$"); // Hex characters only
    }

    #endregion
}
