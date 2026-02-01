using NOIR.Application.Features.Products.Commands.ReorderProductImages;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products.Commands.ReorderProductImages;

/// <summary>
/// Unit tests for ReorderProductImagesCommandHandler.
/// Tests reordering product images with mocked dependencies.
/// </summary>
public class ReorderProductImagesCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ReorderProductImagesCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public ReorderProductImagesCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new ReorderProductImagesCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static ReorderProductImagesCommand CreateTestCommand(
        Guid? productId = null,
        List<ImageSortOrderItem>? items = null)
    {
        return new ReorderProductImagesCommand(
            productId ?? Guid.NewGuid(),
            items ?? []);
    }

    private static Product CreateTestProduct(
        string name = "Test Product",
        string slug = "test-product")
    {
        return Product.Create(name, slug, 99.99m, "VND", TestTenantId);
    }

    private static Product CreateTestProductWithImages()
    {
        var product = CreateTestProduct();
        product.AddImage("https://example.com/img1.jpg", "Image 1", true);
        product.AddImage("https://example.com/img2.jpg", "Image 2", false);
        product.AddImage("https://example.com/img3.jpg", "Image 3", false);
        return product;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidData_ShouldReorderImages()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var imageIds = existingProduct.Images.Select(i => i.Id).ToList();

        var items = new List<ImageSortOrderItem>
        {
            new(imageIds[0], 2),
            new(imageIds[1], 0),
            new(imageIds[2], 1)
        };
        var command = CreateTestCommand(productId, items);

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
        result.IsSuccess.Should().BeTrue();
        existingProduct.Images.First(i => i.Id == imageIds[0]).SortOrder.Should().Be(2);
        existingProduct.Images.First(i => i.Id == imageIds[1]).SortOrder.Should().Be(0);
        existingProduct.Images.First(i => i.Id == imageIds[2]).SortOrder.Should().Be(1);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithPartialReorder_ShouldUpdateOnlySpecifiedImages()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var imageIds = existingProduct.Images.Select(i => i.Id).ToList();

        // Only reorder the first image
        var items = new List<ImageSortOrderItem>
        {
            new(imageIds[0], 5)
        };
        var command = CreateTestCommand(productId, items);

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
        result.IsSuccess.Should().BeTrue();
        existingProduct.Images.First(i => i.Id == imageIds[0]).SortOrder.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WithEmptyItems_ShouldSucceedWithNoChanges()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var command = CreateTestCommand(productId, []);

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
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithSameSortOrders_ShouldSucceed()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var imageIds = existingProduct.Images.Select(i => i.Id).ToList();

        // Set same sort order for multiple images
        var items = new List<ImageSortOrderItem>
        {
            new(imageIds[0], 0),
            new(imageIds[1], 0),
            new(imageIds[2], 0)
        };
        var command = CreateTestCommand(productId, items);

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
        result.IsSuccess.Should().BeTrue();
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
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-PRODUCT-026");
        result.Error.Message.Should().Contain("not found");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Validation Scenarios

    [Fact]
    public async Task Handle_WithInvalidImageId_ShouldReturnValidationError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var invalidImageId = Guid.NewGuid();

        var items = new List<ImageSortOrderItem>
        {
            new(invalidImageId, 0) // Invalid ID
        };
        var command = CreateTestCommand(productId, items);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-PRODUCT-033");
        result.Error.Message.Should().Contain("Invalid image IDs");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithMixedValidAndInvalidIds_ShouldReturnValidationError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var validImageId = existingProduct.Images.First().Id;
        var invalidImageId = Guid.NewGuid();

        var items = new List<ImageSortOrderItem>
        {
            new(validImageId, 0),
            new(invalidImageId, 1)
        };
        var command = CreateTestCommand(productId, items);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-PRODUCT-033");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var items = new List<ImageSortOrderItem>();
        var command = CreateTestCommand(productId, items);
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
    public async Task Handle_WithNegativeSortOrder_ShouldSetNegativeSortOrder()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var imageId = existingProduct.Images.First().Id;

        var items = new List<ImageSortOrderItem>
        {
            new(imageId, -1)
        };
        var command = CreateTestCommand(productId, items);

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
        result.IsSuccess.Should().BeTrue();
        existingProduct.Images.First(i => i.Id == imageId).SortOrder.Should().Be(-1);
    }

    [Fact]
    public async Task Handle_WithLargeSortOrder_ShouldSetLargeSortOrder()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var imageId = existingProduct.Images.First().Id;

        var items = new List<ImageSortOrderItem>
        {
            new(imageId, int.MaxValue)
        };
        var command = CreateTestCommand(productId, items);

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
        result.IsSuccess.Should().BeTrue();
        existingProduct.Images.First(i => i.Id == imageId).SortOrder.Should().Be(int.MaxValue);
    }

    #endregion
}
