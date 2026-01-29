using NOIR.Application.Features.ProductAttributes.DTOs;
using NOIR.Application.Features.ProductAttributes.Queries.GetProductAttributeFormSchema;
using NOIR.Application.Features.ProductAttributes.Specifications;

namespace NOIR.Application.UnitTests.Features.ProductAttributes;

/// <summary>
/// Unit tests for GetProductAttributeFormSchemaQueryHandler.
/// Tests getting the form schema for a product's attributes.
/// </summary>
public class GetProductAttributeFormSchemaQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IRepository<ProductCategory, Guid>> _categoryRepositoryMock;
    private readonly Mock<IRepository<ProductAttribute, Guid>> _attributeRepositoryMock;
    private readonly GetProductAttributeFormSchemaQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetProductAttributeFormSchemaQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _categoryRepositoryMock = new Mock<IRepository<ProductCategory, Guid>>();
        _attributeRepositoryMock = new Mock<IRepository<ProductAttribute, Guid>>();

        _handler = new GetProductAttributeFormSchemaQueryHandler(
            _dbContextMock.Object,
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _attributeRepositoryMock.Object);
    }

    private static Product CreateTestProduct(string name = "Test Product", string slug = "test-product")
    {
        return Product.Create(name, slug, 99.99m, "VND", TestTenantId);
    }

    private static ProductCategory CreateTestCategory(string name = "Test Category", string slug = "test-category")
    {
        return ProductCategory.Create(name, slug, null, TestTenantId);
    }

    private static ProductAttribute CreateTestAttribute(
        string code = "test_attr",
        string name = "Test Attribute",
        AttributeType type = AttributeType.Text)
    {
        return ProductAttribute.Create(code, name, type, TestTenantId);
    }

    private void SetupEmptyDbSets()
    {
        var emptyCategoryAttrs = new List<CategoryAttribute>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(emptyCategoryAttrs.Object);

        var emptyAssignments = new List<ProductAttributeAssignment>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductAttributeAssignments).Returns(emptyAssignments.Object);
    }

    #endregion

    #region Product Not Found Tests

    [Fact]
    public async Task Handle_WithInvalidProductId_ReturnsNotFoundError()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var query = new GetProductAttributeFormSchemaQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.Product.NotFound);
    }

    #endregion

    #region Product Without Category Tests

    [Fact]
    public async Task Handle_ProductWithoutCategory_ReturnsAllActiveAttributes()
    {
        // Arrange
        var product = CreateTestProduct();
        var attribute = CreateTestAttribute("color", "Color");

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        SetupEmptyDbSets();

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ActiveProductAttributesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { attribute });

        var query = new GetProductAttributeFormSchemaQuery(product.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ProductId.Should().Be(product.Id);
        result.Value.ProductName.Should().Be(product.Name);
        result.Value.CategoryId.Should().BeNull();
        result.Value.CategoryName.Should().BeNull();
    }

    #endregion

    #region Product With Category Tests

    [Fact]
    public async Task Handle_ProductWithCategory_ReturnsCategoryLinkedAttributes()
    {
        // Arrange
        var category = CreateTestCategory("Electronics", "electronics");
        var product = CreateTestProduct();

        // Use reflection to set CategoryId since it's a private setter
        typeof(Product).GetProperty("CategoryId")!
            .SetValue(product, category.Id);

        var attribute = CreateTestAttribute("warranty", "Warranty");

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Setup CategoryAttributes with linked attribute
        var categoryAttrs = new List<CategoryAttribute>
        {
            CategoryAttribute.Create(category.Id, attribute.Id, false, 0, TestTenantId)
        }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(categoryAttrs.Object);

        var emptyAssignments = new List<ProductAttributeAssignment>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductAttributeAssignments).Returns(emptyAssignments.Object);

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesByIdsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { attribute });

        var query = new GetProductAttributeFormSchemaQuery(product.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryId.Should().Be(category.Id);
        result.Value.CategoryName.Should().Be(category.Name);
    }

    [Fact]
    public async Task Handle_CategoryWithNoLinkedAttributes_FallsBackToAllAttributes()
    {
        // Arrange
        var category = CreateTestCategory("Empty Category", "empty-category");
        var product = CreateTestProduct();

        typeof(Product).GetProperty("CategoryId")!
            .SetValue(product, category.Id);

        var attribute = CreateTestAttribute("fallback", "Fallback Attr");

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Setup empty CategoryAttributes (no attributes linked to category)
        var emptyCategoryAttrs = new List<CategoryAttribute>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(emptyCategoryAttrs.Object);

        var emptyAssignments = new List<ProductAttributeAssignment>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductAttributeAssignments).Returns(emptyAssignments.Object);

        // When category has no linked attributes, the handler falls back to all active attributes
        // Since categoryAttributeIds is empty, ProductAttributesByIdsSpec is NOT called
        // Only ActiveProductAttributesSpec is called for fallback
        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ActiveProductAttributesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { attribute });

        var query = new GetProductAttributeFormSchemaQuery(product.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Fields.Should().NotBeEmpty();
    }

    #endregion

    #region Schema Response Tests

    [Fact]
    public async Task Handle_ReturnsCorrectProductInfo()
    {
        // Arrange
        var product = CreateTestProduct("My Product", "my-product");

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        SetupEmptyDbSets();

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ActiveProductAttributesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute>());

        var query = new GetProductAttributeFormSchemaQuery(product.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ProductId.Should().Be(product.Id);
        result.Value.ProductName.Should().Be("My Product");
    }

    [Fact]
    public async Task Handle_WithNoAttributes_ReturnsEmptyFields()
    {
        // Arrange
        var product = CreateTestProduct();

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        SetupEmptyDbSets();

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ActiveProductAttributesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute>());

        var query = new GetProductAttributeFormSchemaQuery(product.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Fields.Should().BeEmpty();
    }

    #endregion
}
