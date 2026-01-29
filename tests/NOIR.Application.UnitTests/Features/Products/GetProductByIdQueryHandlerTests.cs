using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Queries.GetProductById;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products;

/// <summary>
/// Unit tests for GetProductByIdQueryHandler.
/// Tests product retrieval by ID and slug scenarios with mocked dependencies.
/// </summary>
public class GetProductByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly GetProductByIdQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetProductByIdQueryHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _currentUserMock = new Mock<ICurrentUser>();

        // Setup default tenant
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new GetProductByIdQueryHandler(
            _productRepositoryMock.Object,
            _currentUserMock.Object);
    }

    private static GetProductByIdQuery CreateTestQuery(Guid? id = null, string? slug = null)
    {
        return new GetProductByIdQuery(id, slug);
    }

    private static Product CreateTestProduct(
        string name = "Test Product",
        string slug = "test-product")
    {
        return Product.Create(name, slug, 99.99m, "VND", TestTenantId);
    }

    #endregion

    #region Success Scenarios - By ID

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var query = CreateTestQuery(id: productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(existingProduct.Name);
        result.Value.Slug.Should().Be(existingProduct.Slug);
        result.Value.BasePrice.Should().Be(existingProduct.BasePrice);
    }

    [Fact]
    public async Task Handle_WithVariantsAndImages_ShouldIncludeAll()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        existingProduct.AddVariant("Variant 1", 49.99m, "SKU-001");
        existingProduct.AddVariant("Variant 2", 59.99m, "SKU-002");
        existingProduct.AddImage("https://example.com/img1.jpg", "Image 1", true);
        existingProduct.AddImage("https://example.com/img2.jpg", "Image 2", false);
        var query = CreateTestQuery(id: productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Variants.Should().HaveCount(2);
        result.Value.Images.Should().HaveCount(2);
    }

    #endregion

    #region Success Scenarios - By Slug

    [Fact]
    public async Task Handle_WithValidSlug_ShouldReturnProduct()
    {
        // Arrange
        var existingProduct = CreateTestProduct();
        var query = CreateTestQuery(slug: "test-product");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductBySlugSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(existingProduct.Name);
        result.Value.Slug.Should().Be(existingProduct.Slug);
    }

    [Fact]
    public async Task Handle_WithSlug_ShouldUseTenantIdFromCurrentUser()
    {
        // Arrange
        var existingProduct = CreateTestProduct();
        var query = CreateTestQuery(slug: "test-product");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductBySlugSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _currentUserMock.Verify(x => x.TenantId, Times.Once);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenProductNotFoundById_ShouldReturnNotFound()
    {
        // Arrange
        var query = CreateTestQuery(id: Guid.NewGuid());

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-PRODUCT-012");
        result.Error.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenProductNotFoundBySlug_ShouldReturnNotFound()
    {
        // Arrange
        var query = CreateTestQuery(slug: "non-existent-product");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductBySlugSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-PRODUCT-012");
        result.Error.Message.Should().Contain("not found");
    }

    #endregion

    #region Validation Scenarios

    [Fact]
    public async Task Handle_WithoutIdOrSlug_ShouldReturnValidationError()
    {
        // Arrange
        var query = CreateTestQuery(id: null, slug: null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-PRODUCT-014");
        result.Error.Message.Should().Contain("ID or Slug");
    }

    [Fact]
    public async Task Handle_WithEmptySlug_ShouldReturnValidationError()
    {
        // Arrange
        var query = CreateTestQuery(id: null, slug: "");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-PRODUCT-014");
    }

    [Fact]
    public async Task Handle_WithWhitespaceSlug_ShouldReturnValidationError()
    {
        // Arrange
        var query = CreateTestQuery(id: null, slug: "   ");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-PRODUCT-014");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var query = CreateTestQuery(id: productId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _productRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductByIdSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithBothIdAndSlug_ShouldPreferIdLookup()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var query = CreateTestQuery(id: productId, slug: "test-product");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Should use ID spec, not slug spec
        _productRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductByIdSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _productRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductBySlugSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldMapAllProductFields()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct("Full Product", "full-product");
        existingProduct.UpdateBasicInfo("Full Product", "full-product", "Short desc", "Description", "<p>HTML</p>");
        existingProduct.SetBrand("Test Brand");
        existingProduct.UpdateIdentification("SKU-001", "BARCODE-001");
        existingProduct.SetInventoryTracking(true);
        existingProduct.UpdateSeo("SEO Title", "SEO Description");
        var query = CreateTestQuery(id: productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Name.Should().Be("Full Product");
        dto.Slug.Should().Be("full-product");
        dto.Description.Should().Be("Description");
        dto.DescriptionHtml.Should().Be("<p>HTML</p>");
        dto.Brand.Should().Be("Test Brand");
        dto.Sku.Should().Be("SKU-001");
        dto.Barcode.Should().Be("BARCODE-001");
        dto.TrackInventory.Should().BeTrue();
        dto.MetaTitle.Should().Be("SEO Title");
        dto.MetaDescription.Should().Be("SEO Description");
    }

    #endregion
}
