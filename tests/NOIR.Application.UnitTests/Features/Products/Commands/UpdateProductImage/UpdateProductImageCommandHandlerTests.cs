using NOIR.Application.Features.Products.Commands.UpdateProductImage;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products.Commands.UpdateProductImage;

/// <summary>
/// Unit tests for UpdateProductImageCommandHandler.
/// Tests updating product images with mocked dependencies.
/// </summary>
public class UpdateProductImageCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly UpdateProductImageCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public UpdateProductImageCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UpdateProductImageCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static UpdateProductImageCommand CreateTestCommand(
        Guid? productId = null,
        Guid? imageId = null,
        string url = "https://example.com/updated-image.jpg",
        string? altText = "Updated Image",
        int sortOrder = 0)
    {
        return new UpdateProductImageCommand(
            productId ?? Guid.NewGuid(),
            imageId ?? Guid.NewGuid(),
            url,
            altText,
            sortOrder);
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
        return product;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidData_ShouldUpdateImage()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var imageId = existingProduct.Images.First().Id;

        var command = CreateTestCommand(
            productId: productId,
            imageId: imageId,
            url: "https://example.com/new-url.jpg",
            altText: "New Alt Text",
            sortOrder: 5);

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

        var updatedImage = existingProduct.Images.First(i => i.Id == imageId);
        updatedImage.Url.ShouldBe("https://example.com/new-url.jpg");
        updatedImage.AltText.ShouldBe("New Alt Text");
        updatedImage.SortOrder.ShouldBe(5);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullAltText_ShouldUpdateWithNullAltText()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var imageId = existingProduct.Images.First().Id;

        var command = CreateTestCommand(
            productId: productId,
            imageId: imageId,
            url: "https://example.com/image.jpg",
            altText: null,
            sortOrder: 0);

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
        existingProduct.Images.First(i => i.Id == imageId).AltText.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_UpdatingUrl_ShouldChangeUrl()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var imageId = existingProduct.Images.First().Id;
        var newUrl = "https://cdn.example.com/products/updated-image.webp";

        var command = CreateTestCommand(
            productId: productId,
            imageId: imageId,
            url: newUrl);

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
        existingProduct.Images.First(i => i.Id == imageId).Url.ShouldBe(newUrl);
    }

    [Fact]
    public async Task Handle_UpdatingSortOrder_ShouldChangeSortOrder()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var imageId = existingProduct.Images.First().Id;

        var command = CreateTestCommand(
            productId: productId,
            imageId: imageId,
            sortOrder: 10);

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
        existingProduct.Images.First(i => i.Id == imageId).SortOrder.ShouldBe(10);
    }

    [Fact]
    public async Task Handle_ShouldReturnProductDto()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var imageId = existingProduct.Images.First().Id;

        var command = CreateTestCommand(productId: productId, imageId: imageId);

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
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBe(existingProduct.Id);
        result.Value.Name.ShouldBe(existingProduct.Name);
    }

    [Fact]
    public async Task Handle_UpdatingNonPrimaryImage_ShouldNotAffectPrimary()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var primaryImageId = existingProduct.Images.First(i => i.IsPrimary).Id;
        var secondImageId = existingProduct.Images.First(i => !i.IsPrimary).Id;

        var command = CreateTestCommand(
            productId: productId,
            imageId: secondImageId,
            url: "https://example.com/updated-secondary.jpg");

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
        existingProduct.Images.First(i => i.Id == primaryImageId).IsPrimary.ShouldBe(true);
        existingProduct.Images.First(i => i.Id == secondImageId).IsPrimary.ShouldBe(false);
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
        result.Error.Code.ShouldBe("NOIR-PRODUCT-027");
        result.Error.Message.ShouldContain("Product");
        result.Error.Message.ShouldContain("not found");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenImageNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var nonExistentImageId = Guid.NewGuid();

        var command = CreateTestCommand(productId: productId, imageId: nonExistentImageId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("NOIR-PRODUCT-028");
        result.Error.Message.ShouldContain("Image");
        result.Error.Message.ShouldContain("not found");

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
        var existingProduct = CreateTestProductWithImages();
        var imageId = existingProduct.Images.First().Id;
        var command = CreateTestCommand(productId: productId, imageId: imageId);
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

        var command = CreateTestCommand(
            productId: productId,
            imageId: imageId,
            sortOrder: -5);

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
        existingProduct.Images.First(i => i.Id == imageId).SortOrder.ShouldBe(-5);
    }

    [Fact]
    public async Task Handle_WithEmptyAltText_ShouldSetEmptyAltText()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var imageId = existingProduct.Images.First().Id;

        var command = CreateTestCommand(
            productId: productId,
            imageId: imageId,
            altText: "");

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
        existingProduct.Images.First(i => i.Id == imageId).AltText.ShouldBe("");
    }

    [Fact]
    public async Task Handle_WithLongUrl_ShouldUpdateWithLongUrl()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var imageId = existingProduct.Images.First().Id;
        var longUrl = "https://example.com/" + new string('a', 500) + ".jpg";

        var command = CreateTestCommand(
            productId: productId,
            imageId: imageId,
            url: longUrl);

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
        existingProduct.Images.First(i => i.Id == imageId).Url.ShouldBe(longUrl);
    }

    #endregion
}
