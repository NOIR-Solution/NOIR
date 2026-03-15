using NOIR.Application.Features.Products.Commands.AddProductImage;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products.Commands.AddProductImage;

/// <summary>
/// Unit tests for AddProductImageCommandHandler.
/// Tests adding images to products with mocked dependencies.
/// </summary>
public class AddProductImageCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly AddProductImageCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public AddProductImageCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new AddProductImageCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static AddProductImageCommand CreateTestCommand(
        Guid? productId = null,
        string url = "https://example.com/image.jpg",
        string? altText = "Test Image",
        int sortOrder = 0,
        bool isPrimary = false)
    {
        return new AddProductImageCommand(
            productId ?? Guid.NewGuid(),
            url,
            altText,
            sortOrder,
            isPrimary);
    }

    private static Product CreateTestProduct(
        string name = "Test Product",
        string slug = "test-product")
    {
        return Product.Create(name, slug, 99.99m, "VND", TestTenantId);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidData_ShouldAddImage()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var command = CreateTestCommand(
            productId: productId,
            url: "https://example.com/new-image.jpg",
            altText: "New Image",
            sortOrder: 1,
            isPrimary: false);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Url.ShouldBe("https://example.com/new-image.jpg");
        result.Value.AltText.ShouldBe("New Image");
        result.Value.SortOrder.ShouldBe(1);
        result.Value.IsPrimary.ShouldBe(false);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithPrimaryFlag_ShouldSetAsPrimary()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var command = CreateTestCommand(
            productId: productId,
            isPrimary: true);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.IsPrimary.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_WithExistingPrimaryImage_ShouldReplacePrimary()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        // Add an existing primary image
        existingProduct.AddImage("https://example.com/existing.jpg", "Existing", true);

        var command = CreateTestCommand(
            productId: productId,
            url: "https://example.com/new-primary.jpg",
            isPrimary: true);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.IsPrimary.ShouldBe(true);
        // The old primary should now be cleared
        existingProduct.Images.Count(i => i.IsPrimary).ShouldBe(1);
    }

    [Fact]
    public async Task Handle_WithNullAltText_ShouldAddImageWithoutAltText()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var command = CreateTestCommand(
            productId: productId,
            altText: null);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.AltText.ShouldBeNull();
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
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("NOIR-PRODUCT-026");
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
        var existingProduct = CreateTestProduct();
        var command = CreateTestCommand(productId: productId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _productRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductByIdForImageUpdateSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCustomSortOrder_ShouldSetCorrectSortOrder()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var command = CreateTestCommand(
            productId: productId,
            sortOrder: 5);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.SortOrder.ShouldBe(5);
    }

    [Fact]
    public async Task Handle_AddingMultipleImages_ShouldMaintainCorrectCount()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        existingProduct.AddImage("https://example.com/image1.jpg", "Image 1", false);
        existingProduct.AddImage("https://example.com/image2.jpg", "Image 2", false);

        var command = CreateTestCommand(
            productId: productId,
            url: "https://example.com/image3.jpg");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        existingProduct.Images.Count().ShouldBe(3);
    }

    #endregion
}
