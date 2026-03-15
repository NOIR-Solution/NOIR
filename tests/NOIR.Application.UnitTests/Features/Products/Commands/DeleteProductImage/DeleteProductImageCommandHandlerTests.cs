using NOIR.Application.Features.Products.Commands.DeleteProductImage;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products.Commands.DeleteProductImage;

/// <summary>
/// Unit tests for DeleteProductImageCommandHandler.
/// Tests deleting images from products with mocked dependencies.
/// </summary>
public class DeleteProductImageCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly DeleteProductImageCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public DeleteProductImageCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeleteProductImageCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static DeleteProductImageCommand CreateTestCommand(
        Guid? productId = null,
        Guid? imageId = null)
    {
        return new DeleteProductImageCommand(
            productId ?? Guid.NewGuid(),
            imageId ?? Guid.NewGuid());
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
        product.AddImage("https://example.com/image1.jpg", "Image 1", true);
        product.AddImage("https://example.com/image2.jpg", "Image 2", false);
        return product;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidImageId_ShouldDeleteImage()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var imageToDelete = existingProduct.Images.First();
        var command = CreateTestCommand(
            productId: productId,
            imageId: imageToDelete.Id);

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
        result.Value.ShouldBe(true);
        existingProduct.Images.Count().ShouldBe(1);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeletePrimaryImage_ShouldSucceed()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var primaryImage = existingProduct.Images.First(i => i.IsPrimary);
        var command = CreateTestCommand(
            productId: productId,
            imageId: primaryImage.Id);

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
    }

    [Fact]
    public async Task Handle_DeleteLastImage_ShouldSucceed()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var image = existingProduct.AddImage("https://example.com/only.jpg", "Only Image", true);
        var command = CreateTestCommand(
            productId: productId,
            imageId: image.Id);

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
        existingProduct.Images.ShouldBeEmpty();
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
        result.Error.Code.ShouldBe("NOIR-PRODUCT-029");
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
        var command = CreateTestCommand(
            productId: productId,
            imageId: nonExistentImageId);

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
        result.Error.Code.ShouldBe("NOIR-PRODUCT-030");
        result.Error.Message.ShouldContain("Image");
        result.Error.Message.ShouldContain("not found");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProductHasNoImages_ShouldReturnNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var someImageId = Guid.NewGuid();
        var command = CreateTestCommand(
            productId: productId,
            imageId: someImageId);

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
        result.Error.Code.ShouldBe("NOIR-PRODUCT-030");
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
        var command = CreateTestCommand(
            productId: productId,
            imageId: imageId);
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
    public async Task Handle_AfterDeletion_ShouldMaintainCorrectImageCount()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        existingProduct.AddImage("https://example.com/image1.jpg", "Image 1", true);
        existingProduct.AddImage("https://example.com/image2.jpg", "Image 2", false);
        existingProduct.AddImage("https://example.com/image3.jpg", "Image 3", false);

        var imageToDelete = existingProduct.Images.Skip(1).First();
        var command = CreateTestCommand(
            productId: productId,
            imageId: imageToDelete.Id);

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
        existingProduct.Images.Count().ShouldBe(2);
        existingProduct.Images.ShouldNotContain(i => i.Id == imageToDelete.Id);
    }

    #endregion
}
