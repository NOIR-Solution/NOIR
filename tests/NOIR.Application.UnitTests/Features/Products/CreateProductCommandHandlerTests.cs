using NOIR.Application.Features.Products.Commands.CreateProduct;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products;

/// <summary>
/// Unit tests for CreateProductCommandHandler.
/// Tests product creation scenarios with mocked dependencies.
/// </summary>
public class CreateProductCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IRepository<ProductCategory, Guid>> _categoryRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly CreateProductCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public CreateProductCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _categoryRepositoryMock = new Mock<IRepository<ProductCategory, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        // Setup default tenant
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new CreateProductCommandHandler(
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private static CreateProductCommand CreateTestCommand(
        string name = "Test Product",
        string slug = "test-product",
        string? shortDescription = null,
        string? description = "Test description",
        string? descriptionHtml = null,
        decimal basePrice = 99.99m,
        string currency = "VND",
        Guid? categoryId = null,
        Guid? brandId = null,
        string? brand = null,
        string? sku = null,
        string? barcode = null,
        bool trackInventory = true,
        string? metaTitle = null,
        string? metaDescription = null,
        int sortOrder = 0,
        List<CreateProductVariantDto>? variants = null,
        List<CreateProductImageDto>? images = null)
    {
        return new CreateProductCommand(
            name,
            slug,
            shortDescription,
            description,
            descriptionHtml,
            basePrice,
            currency,
            categoryId,
            brandId,
            brand,
            sku,
            barcode,
            trackInventory,
            metaTitle,
            metaDescription,
            sortOrder,
            variants,
            images);
    }

    private static ProductCategory CreateTestCategory(
        string name = "Test Category",
        string slug = "test-category")
    {
        return ProductCategory.Create(name, slug, null, TestTenantId);
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
    public async Task Handle_WithValidData_ShouldCreateProduct()
    {
        // Arrange
        var command = CreateTestCommand();

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

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
        result.Value.Name.Should().Be(command.Name);
        result.Value.Slug.Should().Be(command.Slug.ToLowerInvariant());
        result.Value.BasePrice.Should().Be(command.BasePrice);
        result.Value.Currency.Should().Be(command.Currency);
        result.Value.Status.Should().Be(ProductStatus.Draft);

        _productRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCategory_ShouldSetCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = CreateTestCategory();
        var command = CreateTestCommand(categoryId: categoryId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

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
        result.Value.CategoryName.Should().Be(category.Name);
        result.Value.CategorySlug.Should().Be(category.Slug);
    }

    [Fact]
    public async Task Handle_WithSku_ShouldSetSku()
    {
        // Arrange
        var command = CreateTestCommand(sku: "TEST-SKU-001");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSkuExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

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
        result.Value.Sku.Should().Be("TEST-SKU-001");
    }

    [Fact]
    public async Task Handle_WithBrand_ShouldSetBrand()
    {
        // Arrange
        var command = CreateTestCommand(brand: "Test Brand");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

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
        result.Value.Brand.Should().Be("Test Brand");
    }

    [Fact]
    public async Task Handle_WithSeoFields_ShouldSetSeoMetadata()
    {
        // Arrange
        var command = CreateTestCommand(
            metaTitle: "SEO Title",
            metaDescription: "SEO Description");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

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
        result.Value.MetaTitle.Should().Be("SEO Title");
        result.Value.MetaDescription.Should().Be("SEO Description");
    }

    [Fact]
    public async Task Handle_WithVariants_ShouldCreateVariants()
    {
        // Arrange
        var variants = new List<CreateProductVariantDto>
        {
            new("Small", "SKU-S", 19.99m, null, 10, null, 0),
            new("Large", "SKU-L", 24.99m, 29.99m, 5, null, 1)
        };
        var command = CreateTestCommand(variants: variants);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

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
        result.Value.Variants.Should().HaveCount(2);
        result.Value.Variants.Should().Contain(v => v.Name == "Small");
        result.Value.Variants.Should().Contain(v => v.Name == "Large");
    }

    [Fact]
    public async Task Handle_WithoutVariants_ShouldCreateDefaultVariant()
    {
        // Arrange
        var command = CreateTestCommand(variants: null);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

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
        result.Value.Variants.Should().HaveCount(1);
        result.Value.Variants.First().Name.Should().Be("Default");
        result.Value.Variants.First().Price.Should().Be(command.BasePrice);
    }

    [Fact]
    public async Task Handle_WithImages_ShouldCreateImages()
    {
        // Arrange
        var images = new List<CreateProductImageDto>
        {
            new("https://example.com/image1.jpg", "Image 1", 0, true),
            new("https://example.com/image2.jpg", "Image 2", 1, false)
        };
        var command = CreateTestCommand(images: images);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

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
        result.Value.Images.Should().HaveCount(2);
        result.Value.Images.Should().Contain(i => i.Url == "https://example.com/image1.jpg" && i.IsPrimary);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var existingProduct = CreateTestProduct();
        var command = CreateTestCommand();

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("NOIR-PRODUCT-010");
        result.Error.Message.Should().Contain("slug");
        result.Error.Message.Should().Contain("already exists");

        _productRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSkuAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var existingProduct = CreateTestProduct();
        var command = CreateTestCommand(sku: "DUPLICATE-SKU");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSkuExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("NOIR-PRODUCT-011");
        result.Error.Message.Should().Contain("SKU");
        result.Error.Message.Should().Contain("already exists");

        _productRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentCategoryId = Guid.NewGuid();
        var command = CreateTestCommand(categoryId: nonExistentCategoryId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-PRODUCT-002");
        result.Error.Message.Should().Contain("Category");
        result.Error.Message.Should().Contain("not found");

        _productRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var command = CreateTestCommand();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

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
            x => x.FirstOrDefaultAsync(It.IsAny<ProductSlugExistsSpec>(), token),
            Times.Once);
        _productRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Product>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUseTenantIdFromCurrentUser()
    {
        // Arrange
        var command = CreateTestCommand();
        Product? capturedProduct = null;

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedProduct.Should().NotBeNull();
        capturedProduct!.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public async Task Handle_WithoutSku_ShouldNotCheckSkuConflict()
    {
        // Arrange
        var command = CreateTestCommand(sku: null);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

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

        _productRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductSkuExistsSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithoutCategory_ShouldNotQueryCategory()
    {
        // Arrange
        var command = CreateTestCommand(categoryId: null);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

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
        result.Value.CategoryId.Should().BeNull();
        result.Value.CategoryName.Should().BeNull();

        _categoryRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductCategoryByIdSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithTrackInventoryFalse_ShouldDisableInventoryTracking()
    {
        // Arrange
        var command = CreateTestCommand(trackInventory: false);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

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
        result.Value.TrackInventory.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithVariantSkus_ShouldCreateVariantsWithSkus()
    {
        // Arrange
        var variants = new List<CreateProductVariantDto>
        {
            new("Small", "SKU-VAR-001", 19.99m, null, 10, null, 0),
            new("Large", "SKU-VAR-002", 24.99m, null, 5, null, 1)
        };
        var command = CreateTestCommand(variants: variants);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

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
        result.Value.Variants.Should().HaveCount(2);
        result.Value.Variants.Should().Contain(v => v.Sku == "SKU-VAR-001");
        result.Value.Variants.Should().Contain(v => v.Sku == "SKU-VAR-002");
    }

    [Fact]
    public async Task Handle_WithVariantsHavingDuplicateSkus_ShouldCreateAllVariants()
    {
        // Arrange
        // Note: Currently the handler does not validate variant SKU uniqueness within the product.
        // This test documents current behavior - if validation is added, this test should be updated.
        var variants = new List<CreateProductVariantDto>
        {
            new("Small", "DUPLICATE-SKU", 19.99m, null, 10, null, 0),
            new("Large", "DUPLICATE-SKU", 24.99m, null, 5, null, 1)
        };
        var command = CreateTestCommand(variants: variants);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        // Currently allows duplicate variant SKUs within same product
        result.IsSuccess.Should().BeTrue();
        result.Value.Variants.Should().HaveCount(2);
        result.Value.Variants.All(v => v.Sku == "DUPLICATE-SKU").Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNullVariantSku_ShouldCreateVariantWithoutSku()
    {
        // Arrange
        var variants = new List<CreateProductVariantDto>
        {
            new("No SKU Variant", null, 19.99m, null, 10, null, 0)
        };
        var command = CreateTestCommand(variants: variants);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

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
        result.Value.Variants.Should().HaveCount(1);
        result.Value.Variants.First().Sku.Should().BeNull();
    }

    #endregion
}
