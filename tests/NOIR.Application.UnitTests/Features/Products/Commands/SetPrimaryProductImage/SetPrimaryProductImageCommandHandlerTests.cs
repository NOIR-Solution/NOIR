using NOIR.Application.Features.Products.Commands.SetPrimaryProductImage;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products.Commands.SetPrimaryProductImage;

/// <summary>
/// Unit tests for SetPrimaryProductImageCommandHandler.
/// Tests setting a product image as primary with mocked dependencies.
/// </summary>
public class SetPrimaryProductImageCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly SetPrimaryProductImageCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public SetPrimaryProductImageCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new SetPrimaryProductImageCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static SetPrimaryProductImageCommand CreateTestCommand(
        Guid? productId = null,
        Guid? imageId = null)
    {
        return new SetPrimaryProductImageCommand(
            productId ?? Guid.NewGuid(),
            imageId ?? Guid.NewGuid());
    }

    private static Product CreateTestProduct(
        string name = "Test Product",
        string slug = "test-product")
    {
        return Product.Create(name, slug, 99.99m, "VND", TestTenantId);
    }

    private static Product CreateTestProductWithImages(bool withPrimary = true)
    {
        var product = CreateTestProduct();
        product.AddImage("https://example.com/img1.jpg", "Image 1", withPrimary);
        product.AddImage("https://example.com/img2.jpg", "Image 2", false);
        product.AddImage("https://example.com/img3.jpg", "Image 3", false);
        return product;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidData_ShouldSetImageAsPrimary()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var secondImageId = existingProduct.Images.ToList()[1].Id;

        var command = CreateTestCommand(productId, secondImageId);

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
        existingProduct.Images.Count(i => i.IsPrimary).Should().Be(1);
        existingProduct.Images.First(i => i.Id == secondImageId).IsPrimary.Should().BeTrue();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingPrimary_ShouldClearPreviousPrimary()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages(withPrimary: true);
        var firstImageId = existingProduct.Images.First().Id;
        var secondImageId = existingProduct.Images.ToList()[1].Id;

        // First image is primary
        existingProduct.Images.First(i => i.Id == firstImageId).IsPrimary.Should().BeTrue();

        var command = CreateTestCommand(productId, secondImageId);

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
        existingProduct.Images.First(i => i.Id == firstImageId).IsPrimary.Should().BeFalse();
        existingProduct.Images.First(i => i.Id == secondImageId).IsPrimary.Should().BeTrue();
        existingProduct.Images.Count(i => i.IsPrimary).Should().Be(1);
    }

    [Fact]
    public async Task Handle_SettingSamePrimary_ShouldStillSucceed()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages(withPrimary: true);
        var primaryImageId = existingProduct.Images.First(i => i.IsPrimary).Id;

        var command = CreateTestCommand(productId, primaryImageId);

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
        existingProduct.Images.Count(i => i.IsPrimary).Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithNoPreviousPrimary_ShouldSetAsPrimary()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages(withPrimary: false);
        var imageId = existingProduct.Images.First().Id;

        var command = CreateTestCommand(productId, imageId);

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
        existingProduct.Images.First(i => i.Id == imageId).IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnProductDto()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var imageId = existingProduct.Images.First().Id;

        var command = CreateTestCommand(productId, imageId);

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
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(existingProduct.Id);
        result.Value.Name.Should().Be(existingProduct.Name);
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
        result.Error.Code.Should().Be("NOIR-PRODUCT-031");
        result.Error.Message.Should().Contain("Product");
        result.Error.Message.Should().Contain("not found");

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

        var command = CreateTestCommand(productId, nonExistentImageId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-PRODUCT-032");
        result.Error.Message.Should().Contain("Image");
        result.Error.Message.Should().Contain("not found");

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
        var command = CreateTestCommand(productId, imageId);
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
    public async Task Handle_WithSingleImage_ShouldSetItAsPrimary()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        existingProduct.AddImage("https://example.com/single.jpg", "Single Image", false);
        var imageId = existingProduct.Images.First().Id;

        var command = CreateTestCommand(productId, imageId);

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
        existingProduct.Images.Single().IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithProductHavingCategory_ShouldIncludeCategoryInfo()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();
        var category = ProductCategory.Create("Electronics", "electronics", null, TestTenantId);
        existingProduct.SetCategory(category.Id);

        // Simulate loaded navigation property by using reflection
        var categoryProperty = typeof(Product).GetProperty("Category");
        if (categoryProperty != null && categoryProperty.CanWrite)
        {
            categoryProperty.SetValue(existingProduct, category);
        }

        var imageId = existingProduct.Images.First().Id;
        var command = CreateTestCommand(productId, imageId);

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
}
