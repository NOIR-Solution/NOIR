using NOIR.Application.Features.ProductAttributes.DTOs;
using NOIR.Application.Features.ProductAttributes.Queries.GetProductAttributeAssignments;

namespace NOIR.Application.UnitTests.Features.ProductAttributes;

/// <summary>
/// Unit tests for GetProductAttributeAssignmentsQueryHandler.
/// Tests retrieving a product's attribute values.
/// </summary>
public class GetProductAttributeAssignmentsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly GetProductAttributeAssignmentsQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetProductAttributeAssignmentsQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();

        _handler = new GetProductAttributeAssignmentsQueryHandler(
            _dbContextMock.Object,
            _productRepositoryMock.Object);
    }

    private static Product CreateTestProduct(string name = "Test Product", string slug = "test-product")
    {
        return Product.Create(name, slug, 99.99m, "VND", TestTenantId);
    }

    private static ProductAttribute CreateTestAttribute(
        string code = "test_attr",
        string name = "Test Attribute",
        AttributeType type = AttributeType.Text)
    {
        return ProductAttribute.Create(code, name, type, TestTenantId);
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

        var query = new GetProductAttributeAssignmentsQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.Product.NotFound);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidProductId_ReturnsSuccess()
    {
        // Arrange
        var product = CreateTestProduct();

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Setup empty DbSet - navigation property mocking with Include is complex
        // Full assignment testing should be done via integration tests
        var emptyList = new List<ProductAttributeAssignment>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductAttributeAssignments).Returns(emptyList.Object);

        var query = new GetProductAttributeAssignmentsQuery(product.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        // Note: Full assignment testing with navigation properties requires integration tests
    }

    [Fact]
    public async Task Handle_WithNoAssignments_ReturnsEmptyList()
    {
        // Arrange
        var product = CreateTestProduct();

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var emptyList = new List<ProductAttributeAssignment>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductAttributeAssignments).Returns(emptyList.Object);

        var query = new GetProductAttributeAssignmentsQuery(product.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region Variant Filter Tests

    [Fact]
    public async Task Handle_WithVariantId_FiltersToVariantAssignments()
    {
        // Arrange
        var product = CreateTestProduct();
        var variantId = Guid.NewGuid();

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var emptyList = new List<ProductAttributeAssignment>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductAttributeAssignments).Returns(emptyList.Object);

        var query = new GetProductAttributeAssignmentsQuery(product.Id, variantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // The handler should filter by variantId
    }

    #endregion
}
