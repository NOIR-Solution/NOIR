using NOIR.Application.Features.Products.Commands.PublishProduct;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products;

/// <summary>
/// Unit tests for PublishProductCommandHandler.
/// Tests product publish scenarios with mocked dependencies.
/// </summary>
public class PublishProductCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IRepository<ProductCategory, Guid>> _categoryRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly PublishProductCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public PublishProductCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _categoryRepositoryMock = new Mock<IRepository<ProductCategory, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new PublishProductCommandHandler(
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static PublishProductCommand CreateTestCommand(Guid? id = null)
    {
        return new PublishProductCommand(id ?? Guid.NewGuid());
    }

    private static ProductCategory CreateTestCategory(
        string name = "Test Category",
        string slug = "test-category")
    {
        return ProductCategory.Create(name, slug, null, TestTenantId);
    }

    private static Product CreateTestProduct(
        string name = "Test Product",
        string slug = "test-product",
        ProductStatus status = ProductStatus.Draft)
    {
        var product = Product.Create(name, slug, 99.99m, "VND", TestTenantId);
        // Product is created in Draft status by default
        if (status == ProductStatus.Active)
        {
            product.Publish();
        }
        else if (status == ProductStatus.Archived)
        {
            product.Archive();
        }
        return product;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDraftProduct_ShouldPublishProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct(status: ProductStatus.Draft);
        var command = CreateTestCommand(id: productId);

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
        result.Value.Status.ShouldBe(ProductStatus.Active);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCategory_ShouldReturnCategoryInfo()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var existingProduct = CreateTestProduct(status: ProductStatus.Draft);
        existingProduct.SetCategory(categoryId);
        var category = CreateTestCategory();
        var command = CreateTestCommand(id: productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.CategoryId.ShouldBe(categoryId);
        result.Value.CategoryName.ShouldBe(category.Name);
        result.Value.CategorySlug.ShouldBe(category.Slug);
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
        result.Error.Code.ShouldBe("NOIR-PRODUCT-012");
        result.Error.Message.ShouldContain("not found");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Validation Scenarios

    [Fact]
    public async Task Handle_WhenProductAlreadyActive_ShouldReturnValidationError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct(status: ProductStatus.Active);
        var command = CreateTestCommand(id: productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("NOIR-PRODUCT-013");
        result.Error.Message.ShouldContain("Draft");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProductArchived_ShouldReturnValidationError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct(status: ProductStatus.Archived);
        var command = CreateTestCommand(id: productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("NOIR-PRODUCT-013");
        result.Error.Message.ShouldContain("Draft");

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
        var existingProduct = CreateTestProduct(status: ProductStatus.Draft);
        var command = CreateTestCommand(id: productId);
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
    public async Task Handle_WithoutCategory_ShouldNotQueryCategory()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct(status: ProductStatus.Draft);
        // CategoryId is null by default
        var command = CreateTestCommand(id: productId);

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
        result.Value.CategoryId.ShouldBeNull();
        result.Value.CategoryName.ShouldBeNull();

        _categoryRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductCategoryByIdSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldIncludeVariantsAndImagesInResult()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct(status: ProductStatus.Draft);
        // Add a variant to the product
        existingProduct.AddVariant("Test Variant", 49.99m, "SKU-001");
        existingProduct.AddImage("https://example.com/image.jpg", "Test Image", true);
        var command = CreateTestCommand(id: productId);

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
        result.Value.Variants.Count().ShouldBe(1);
        result.Value.Variants.First().Name.ShouldBe("Test Variant");
        result.Value.Images.Count().ShouldBe(1);
        result.Value.Images.First().Url.ShouldBe("https://example.com/image.jpg");
    }

    #endregion
}
