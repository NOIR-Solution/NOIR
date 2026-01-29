using FluentAssertions;
using NOIR.Application.Features.ProductAttributes.Queries.GetProductAttributeFormSchema;
using NOIR.Domain.Entities.Product;
using Xunit;

namespace NOIR.Application.UnitTests.Features.ProductAttributes;

/// <summary>
/// Unit tests for GetProductAttributeFormSchemaQueryHandler.
/// Phase 9: Product Form Attribute Integration
/// </summary>
public class ProductAttributeFormSchemaQueryHandlerTests
{
    private readonly Mock<IRepository<Product, Guid>> _productRepository;
    private readonly Mock<IRepository<ProductCategory, Guid>> _categoryRepository;
    private readonly Mock<IRepository<ProductAttribute, Guid>> _attributeRepository;
    private readonly Mock<IApplicationDbContext> _dbContext;
    private readonly GetProductAttributeFormSchemaQueryHandler _handler;

    public ProductAttributeFormSchemaQueryHandlerTests()
    {
        _productRepository = new Mock<IRepository<Product, Guid>>();
        _categoryRepository = new Mock<IRepository<ProductCategory, Guid>>();
        _attributeRepository = new Mock<IRepository<ProductAttribute, Guid>>();
        _dbContext = new Mock<IApplicationDbContext>();

        _handler = new GetProductAttributeFormSchemaQueryHandler(
            _dbContext.Object,
            _productRepository.Object,
            _categoryRepository.Object,
            _attributeRepository.Object);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _productRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var query = new GetProductAttributeFormSchemaQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Product.NotFound);
    }

    [Fact]
    public async Task Handle_ProductWithCategory_ReturnsAttributesForCategory()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();

        var product = CreateProduct(productId, "Test Product", categoryId);
        var category = CreateCategory(categoryId, "Electronics");
        var attribute = CreateAttribute(attributeId, "Color", AttributeType.Select);

        _productRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _categoryRepository.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Setup category attributes linking
        var categoryAttribute = CategoryAttribute.Create(categoryId, attributeId, isRequired: true);
        SetupCategoryAttributesDbSet(new List<CategoryAttribute> { categoryAttribute });

        // Setup attributes repository
        _attributeRepository.Setup(r => r.ListAsync(It.IsAny<ISpecification<ProductAttribute>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { attribute });

        // Setup empty assignments
        SetupProductAttributeAssignmentsDbSet(new List<ProductAttributeAssignment>());

        var query = new GetProductAttributeFormSchemaQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ProductId.Should().Be(productId);
        result.Value.CategoryName.Should().Be("Electronics");
        result.Value.Fields.Should().HaveCount(1);

        var field = result.Value.Fields.First();
        field.Code.Should().Be("color");
        field.IsRequired.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ProductWithoutCategory_ReturnsAllActiveAttributes()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();

        var product = CreateProduct(productId, "Test Product", categoryId: null);
        var attribute = CreateAttribute(attributeId, "Material", AttributeType.Text);

        _productRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Setup category attributes (empty for no category)
        SetupCategoryAttributesDbSet(new List<CategoryAttribute>());

        // Setup attributes repository to return all active attributes
        _attributeRepository.Setup(r => r.ListAsync(It.IsAny<ISpecification<ProductAttribute>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { attribute });

        // Setup empty assignments
        SetupProductAttributeAssignmentsDbSet(new List<ProductAttributeAssignment>());

        var query = new GetProductAttributeFormSchemaQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryId.Should().BeNull();
        result.Value.CategoryName.Should().BeNull();
        result.Value.Fields.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ProductWithExistingValues_IncludesCurrentValues()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();

        var product = CreateProduct(productId, "Test Product", categoryId);
        var category = CreateCategory(categoryId, "Electronics");
        var attribute = CreateAttribute(attributeId, "Brand", AttributeType.Text);

        _productRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _categoryRepository.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Setup category attributes
        var categoryAttribute = CategoryAttribute.Create(categoryId, attributeId, isRequired: false);
        SetupCategoryAttributesDbSet(new List<CategoryAttribute> { categoryAttribute });

        _attributeRepository.Setup(r => r.ListAsync(It.IsAny<ISpecification<ProductAttribute>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { attribute });

        // Setup existing assignment with value
        var assignment = ProductAttributeAssignment.Create(productId, attributeId);
        assignment.SetTextValue("Samsung");
        SetupProductAttributeAssignmentsDbSet(new List<ProductAttributeAssignment> { assignment });

        var query = new GetProductAttributeFormSchemaQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Fields.Should().HaveCount(1);

        var field = result.Value.Fields.First();
        field.CurrentValue.Should().Be("Samsung");
    }

    [Fact]
    public async Task Handle_VariantSpecific_ReturnsVariantValues()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();

        var product = CreateProduct(productId, "Test Product", categoryId);
        var category = CreateCategory(categoryId, "Clothing");
        var attribute = CreateAttribute(attributeId, "Size", AttributeType.Text);

        _productRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _categoryRepository.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Setup category attributes
        var categoryAttribute = CategoryAttribute.Create(categoryId, attributeId, isRequired: true);
        SetupCategoryAttributesDbSet(new List<CategoryAttribute> { categoryAttribute });

        _attributeRepository.Setup(r => r.ListAsync(It.IsAny<ISpecification<ProductAttribute>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { attribute });

        // Setup variant-specific assignment
        var variantAssignment = ProductAttributeAssignment.Create(productId, attributeId, variantId);
        variantAssignment.SetTextValue("Large");
        SetupProductAttributeAssignmentsDbSet(new List<ProductAttributeAssignment> { variantAssignment });

        var query = new GetProductAttributeFormSchemaQuery(productId, variantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Fields.Should().HaveCount(1);

        var field = result.Value.Fields.First();
        field.CurrentValue.Should().Be("Large");
    }

    [Fact]
    public async Task Handle_CategoryWithNoAttributes_FallsBackToAllActiveAttributes()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();

        var product = CreateProduct(productId, "Test Product", categoryId);
        var category = CreateCategory(categoryId, "New Category");
        var attribute = CreateAttribute(attributeId, "Weight", AttributeType.Decimal);

        _productRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _categoryRepository.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Setup empty category attributes (no attributes linked to this category)
        SetupCategoryAttributesDbSet(new List<CategoryAttribute>());

        // The handler calls ListAsync twice:
        // 1. First with ProductAttributesByIdsSpec (empty IDs) - returns empty
        // 2. Second with ActiveProductAttributesSpec - returns all active attributes
        // Since there are no category attribute IDs, it skips the first call and goes directly
        // to the fallback. So we just need to return the attribute on any call.
        _attributeRepository.Setup(r => r.ListAsync(It.IsAny<ISpecification<ProductAttribute>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { attribute });

        SetupProductAttributeAssignmentsDbSet(new List<ProductAttributeAssignment>());

        var query = new GetProductAttributeFormSchemaQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // When category has no linked attributes, the handler falls back to all active attributes
        result.Value.Fields.Should().NotBeEmpty();
        result.Value.Fields.First().Code.Should().Be("weight");
    }

    #region Helper Methods

    private static Product CreateProduct(Guid id, string name, Guid? categoryId)
    {
        var product = Product.Create(name, name.ToLowerInvariant().Replace(" ", "-"), 100m, "USD");
        // Use reflection to set Id since it's normally set by EF
        typeof(Product).GetProperty("Id")!.SetValue(product, id);
        if (categoryId.HasValue)
        {
            product.SetCategory(categoryId.Value);
        }
        return product;
    }

    private static ProductCategory CreateCategory(Guid id, string name)
    {
        var category = ProductCategory.Create(name, name.ToLowerInvariant().Replace(" ", "-"));
        typeof(ProductCategory).GetProperty("Id")!.SetValue(category, id);
        return category;
    }

    private static ProductAttribute CreateAttribute(Guid id, string name, AttributeType type)
    {
        var attribute = ProductAttribute.Create(
            name.ToLowerInvariant().Replace(" ", "_"),
            name,
            type);
        typeof(ProductAttribute).GetProperty("Id")!.SetValue(attribute, id);
        return attribute;
    }

    private void SetupCategoryAttributesDbSet(List<CategoryAttribute> categoryAttributes)
    {
        var mockDbSet = categoryAttributes.BuildMockDbSet();
        _dbContext.Setup(c => c.CategoryAttributes).Returns(mockDbSet.Object);
    }

    private void SetupProductAttributeAssignmentsDbSet(List<ProductAttributeAssignment> assignments)
    {
        var mockDbSet = assignments.BuildMockDbSet();
        _dbContext.Setup(c => c.ProductAttributeAssignments).Returns(mockDbSet.Object);
    }

    #endregion
}
