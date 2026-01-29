using NOIR.Application.Features.Products.Commands.BulkDeleteProducts;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products;

/// <summary>
/// Unit tests for BulkDeleteProductsCommandHandler.
/// Tests bulk product soft-delete scenarios with mocked dependencies.
/// </summary>
public class BulkDeleteProductsCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly BulkDeleteProductsCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public BulkDeleteProductsCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new BulkDeleteProductsCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static BulkDeleteProductsCommand CreateTestCommand(List<Guid>? productIds = null)
    {
        return new BulkDeleteProductsCommand(productIds ?? new List<Guid> { Guid.NewGuid() });
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

        // Set status
        if (status == ProductStatus.Active)
        {
            product.Publish();
        }
        else if (status == ProductStatus.Archived)
        {
            product.Publish();
            product.Archive();
        }
        return product;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithAllExistingProducts_ShouldDeleteAll()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var productId3 = Guid.NewGuid();
        var productIds = new List<Guid> { productId1, productId2, productId3 };

        var product1 = CreateTestProduct(productId1, "Product 1", "product-1", ProductStatus.Draft);
        var product2 = CreateTestProduct(productId2, "Product 2", "product-2", ProductStatus.Active);
        var product3 = CreateTestProduct(productId3, "Product 3", "product-3", ProductStatus.Archived);

        var command = CreateTestCommand(productIds);

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product1, product2, product3 });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().Be(3);
        result.Value.Failed.Should().Be(0);
        result.Value.Errors.Should().BeEmpty();

        // Verify Remove was called for each product
        _productRepositoryMock.Verify(
            x => x.Remove(It.IsAny<Product>()),
            Times.Exactly(3));

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSingleProduct_ShouldDelete()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateTestProduct(productId, "Test Product", "test-product", ProductStatus.Draft);
        var command = CreateTestCommand(new List<Guid> { productId });

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().Be(1);
        result.Value.Failed.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldDeleteProductsInAnyStatus()
    {
        // Arrange - delete should work regardless of status (Draft, Active, Archived)
        var draftId = Guid.NewGuid();
        var activeId = Guid.NewGuid();
        var archivedId = Guid.NewGuid();
        var productIds = new List<Guid> { draftId, activeId, archivedId };

        var draftProduct = CreateTestProduct(draftId, "Draft Product", "draft", ProductStatus.Draft);
        var activeProduct = CreateTestProduct(activeId, "Active Product", "active", ProductStatus.Active);
        var archivedProduct = CreateTestProduct(archivedId, "Archived Product", "archived", ProductStatus.Archived);

        var command = CreateTestCommand(productIds);

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { draftProduct, activeProduct, archivedProduct });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - all products deleted regardless of status
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().Be(3);
        result.Value.Failed.Should().Be(0);
    }

    #endregion

    #region Partial Success Scenarios

    [Fact]
    public async Task Handle_WithSomeNonExistentProducts_ShouldReturnPartialSuccess()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();
        var productIds = new List<Guid> { existingId, nonExistentId };

        var existingProduct = CreateTestProduct(existingId, "Existing Product", "existing-product", ProductStatus.Draft);
        var command = CreateTestCommand(productIds);

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { existingProduct }); // Only return existing product

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().Be(1);
        result.Value.Failed.Should().Be(1);
        result.Value.Errors.Should().HaveCount(1);
        result.Value.Errors[0].ProductId.Should().Be(nonExistentId);
        result.Value.Errors[0].Message.Should().Contain("not found");
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WithAllNonExistentProducts_ShouldReturnAllErrors()
    {
        // Arrange
        var nonExistentId1 = Guid.NewGuid();
        var nonExistentId2 = Guid.NewGuid();
        var productIds = new List<Guid> { nonExistentId1, nonExistentId2 };

        var command = CreateTestCommand(productIds);

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>()); // Return empty list

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Operation itself succeeds, just no products to delete
        result.Value.Success.Should().Be(0);
        result.Value.Failed.Should().Be(2);
        result.Value.Errors.Should().HaveCount(2);
        result.Value.Errors.Should().AllSatisfy(e => e.Message.Should().Contain("not found"));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithEmptyProductIds_ShouldReturnEmptyResult()
    {
        // Arrange
        var command = CreateTestCommand(new List<Guid>());

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().Be(0);
        result.Value.Failed.Should().Be(0);
        result.Value.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateTestProduct(productId, "Test Product", "test-product", ProductStatus.Draft);
        var command = CreateTestCommand(new List<Guid> { productId });
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _productRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductsByIdsForUpdateSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSaveChangesOnlyOnce()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var productIds = new List<Guid> { productId1, productId2 };

        var product1 = CreateTestProduct(productId1, "Product 1", "product-1", ProductStatus.Draft);
        var product2 = CreateTestProduct(productId2, "Product 2", "product-2", ProductStatus.Draft);

        var command = CreateTestCommand(productIds);

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product1, product2 });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - SaveChangesAsync should be called only once after processing all products
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryRemoveForEachProduct()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var productIds = new List<Guid> { productId1, productId2 };

        var product1 = CreateTestProduct(productId1, "Product 1", "product-1", ProductStatus.Draft);
        var product2 = CreateTestProduct(productId2, "Product 2", "product-2", ProductStatus.Draft);

        var command = CreateTestCommand(productIds);

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product1, product2 });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Remove should be called for each product
        _productRepositoryMock.Verify(
            x => x.Remove(product1),
            Times.Once);
        _productRepositoryMock.Verify(
            x => x.Remove(product2),
            Times.Once);
    }

    #endregion
}
