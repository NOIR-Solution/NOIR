using NOIR.Application.Features.Products.Commands.DeleteProductCategory;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products;

/// <summary>
/// Unit tests for DeleteProductCategoryCommandHandler.
/// Tests category deletion scenarios with mocked dependencies.
/// </summary>
public class DeleteProductCategoryCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductCategory, Guid>> _categoryRepositoryMock;
    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly DeleteProductCategoryCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public DeleteProductCategoryCommandHandlerTests()
    {
        _categoryRepositoryMock = new Mock<IRepository<ProductCategory, Guid>>();
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeleteProductCategoryCommandHandler(
            _categoryRepositoryMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static DeleteProductCategoryCommand CreateTestCommand(Guid? id = null)
    {
        return new DeleteProductCategoryCommand(id ?? Guid.NewGuid());
    }

    private static ProductCategory CreateTestCategory(
        string name = "Test Category",
        string slug = "test-category",
        Guid? parentId = null)
    {
        return ProductCategory.Create(name, slug, parentId, TestTenantId);
    }

    private static Product CreateTestProduct(
        string name = "Test Product",
        string slug = "test-product")
    {
        return Product.Create(name, slug, 99.99m, "USD", TestTenantId);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCategory_ShouldDeleteCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory();
        var command = CreateTestCommand(id: categoryId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductCategory>()); // No child categories

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductCategoryHasProductsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>()); // No products

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        _categoryRepositoryMock.Verify(
            x => x.Remove(existingCategory),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = CreateTestCommand();

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-PRODUCT-003");
        result.Error.Message.Should().Contain("not found");

        _categoryRepositoryMock.Verify(
            x => x.Remove(It.IsAny<ProductCategory>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenCategoryHasChildren_ShouldReturnConflict()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory();
        var childCategory = CreateTestCategory("Child Category", "child-category", parentId: categoryId);
        var command = CreateTestCommand(id: categoryId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductCategory> { childCategory }); // Has child category

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("NOIR-PRODUCT-005");
        result.Error.Message.Should().Contain("child categories");

        _categoryRepositoryMock.Verify(
            x => x.Remove(It.IsAny<ProductCategory>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCategoryHasProducts_ShouldReturnConflict()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory();
        var product = CreateTestProduct();
        var command = CreateTestCommand(id: categoryId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductCategory>()); // No child categories

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductCategoryHasProductsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product }); // Has products

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("NOIR-PRODUCT-006");
        result.Error.Message.Should().Contain("products");

        _categoryRepositoryMock.Verify(
            x => x.Remove(It.IsAny<ProductCategory>()),
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
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory();
        var command = CreateTestCommand(id: categoryId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductCategory>());

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductCategoryHasProductsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _categoryRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductCategoryByIdForUpdateSpec>(), token),
            Times.Once);
        _categoryRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductCategoriesSpec>(), token),
            Times.Once);
        _productRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductCategoryHasProductsSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCheckChildrenBeforeProducts()
    {
        // Arrange - Category has both children and products
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory();
        var childCategory = CreateTestCategory("Child Category", "child-category", parentId: categoryId);
        var command = CreateTestCommand(id: categoryId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductCategory> { childCategory }); // Has child

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("NOIR-PRODUCT-005"); // Children error first

        // Should not check for products if children exist
        _productRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductCategoryHasProductsSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion
}
