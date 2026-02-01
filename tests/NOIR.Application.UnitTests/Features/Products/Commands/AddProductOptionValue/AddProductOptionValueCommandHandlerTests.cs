using NOIR.Application.Features.Products.Commands.AddProductOptionValue;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products.Commands.AddProductOptionValue;

/// <summary>
/// Unit tests for AddProductOptionValueCommandHandler.
/// Tests adding values to product options with mocked dependencies.
/// </summary>
public class AddProductOptionValueCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly AddProductOptionValueCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public AddProductOptionValueCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new AddProductOptionValueCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static AddProductOptionValueCommand CreateTestCommand(
        Guid? productId = null,
        Guid? optionId = null,
        string value = "Red",
        string? displayValue = "Red",
        string? colorCode = null,
        string? swatchUrl = null,
        int sortOrder = 0)
    {
        return new AddProductOptionValueCommand(
            productId ?? Guid.NewGuid(),
            optionId ?? Guid.NewGuid(),
            value,
            displayValue,
            colorCode,
            swatchUrl,
            sortOrder);
    }

    private static Product CreateTestProductWithOption(
        string name = "Test Product",
        string slug = "test-product",
        string optionName = "Color")
    {
        var product = Product.Create(name, slug, 99.99m, "VND", TestTenantId);
        product.AddOption(optionName, optionName);
        return product;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidData_ShouldAddOptionValue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOption();
        var optionId = existingProduct.Options.First().Id;
        var command = CreateTestCommand(
            productId: productId,
            optionId: optionId,
            value: "Blue",
            displayValue: "Blue");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForOptionUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("blue");
        result.Value.DisplayValue.Should().Be("Blue");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithColorCode_ShouldSetColorCode()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOption();
        var optionId = existingProduct.Options.First().Id;
        var command = CreateTestCommand(
            productId: productId,
            optionId: optionId,
            value: "Green",
            colorCode: "#00FF00");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForOptionUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ColorCode.Should().Be("#00FF00");
    }

    [Fact]
    public async Task Handle_WithSwatchUrl_ShouldSetSwatchUrl()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOption();
        var optionId = existingProduct.Options.First().Id;
        var command = CreateTestCommand(
            productId: productId,
            optionId: optionId,
            value: "Pattern",
            swatchUrl: "https://example.com/pattern-swatch.jpg");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForOptionUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SwatchUrl.Should().Be("https://example.com/pattern-swatch.jpg");
    }

    [Fact]
    public async Task Handle_ShouldNormalizeValue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOption();
        var optionId = existingProduct.Options.First().Id;
        var command = CreateTestCommand(
            productId: productId,
            optionId: optionId,
            value: "Light Blue",
            displayValue: "Light Blue");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForOptionUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("light_blue");
        result.Value.DisplayValue.Should().Be("Light Blue");
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
                It.IsAny<ProductByIdForOptionUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-PRODUCT-021");
        result.Error.Message.Should().Contain("Product");
        result.Error.Message.Should().Contain("not found");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOptionNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOption();
        var nonExistentOptionId = Guid.NewGuid();
        var command = CreateTestCommand(
            productId: productId,
            optionId: nonExistentOptionId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForOptionUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-PRODUCT-051");
        result.Error.Message.Should().Contain("Option");
        result.Error.Message.Should().Contain("not found");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Validation Scenarios

    [Fact]
    public async Task Handle_WhenValueAlreadyExists_ShouldReturnValidationError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOption();
        var option = existingProduct.Options.First();
        // Add an existing value
        option.AddValue("Red", "Red");

        var command = CreateTestCommand(
            productId: productId,
            optionId: option.Id,
            value: "Red");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForOptionUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-PRODUCT-052");
        result.Error.Message.Should().Contain("already exists");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDifferentValue_ShouldSucceed()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOption();
        var option = existingProduct.Options.First();
        option.AddValue("Red", "Red");

        var command = CreateTestCommand(
            productId: productId,
            optionId: option.Id,
            value: "Blue");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForOptionUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        option.Values.Should().HaveCount(2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOption();
        var optionId = existingProduct.Options.First().Id;
        var command = CreateTestCommand(
            productId: productId,
            optionId: optionId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForOptionUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _productRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductByIdForOptionUpdateSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullColorCodeAndSwatchUrl_ShouldNotSetProperties()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOption();
        var optionId = existingProduct.Options.First().Id;
        var command = CreateTestCommand(
            productId: productId,
            optionId: optionId,
            value: "Plain",
            colorCode: null,
            swatchUrl: null);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForOptionUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ColorCode.Should().BeNull();
        result.Value.SwatchUrl.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AddingMultipleValues_ShouldMaintainCorrectCount()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOption();
        var option = existingProduct.Options.First();
        option.AddValue("Red", "Red");
        option.AddValue("Green", "Green");

        var command = CreateTestCommand(
            productId: productId,
            optionId: option.Id,
            value: "Blue");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForOptionUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        option.Values.Should().HaveCount(3);
    }

    #endregion
}
