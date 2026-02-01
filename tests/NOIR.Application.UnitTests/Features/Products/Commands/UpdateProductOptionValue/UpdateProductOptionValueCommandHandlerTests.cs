using NOIR.Application.Features.Products.Commands.UpdateProductOptionValue;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products.Commands.UpdateProductOptionValue;

/// <summary>
/// Unit tests for UpdateProductOptionValueCommandHandler.
/// Tests updating product option values with mocked dependencies.
/// </summary>
public class UpdateProductOptionValueCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdateProductOptionValueCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public UpdateProductOptionValueCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UpdateProductOptionValueCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static UpdateProductOptionValueCommand CreateTestCommand(
        Guid? productId = null,
        Guid? optionId = null,
        Guid? valueId = null,
        string value = "updated_value",
        string? displayValue = "Updated Value",
        string? colorCode = null,
        string? swatchUrl = null,
        int sortOrder = 0)
    {
        return new UpdateProductOptionValueCommand(
            productId ?? Guid.NewGuid(),
            optionId ?? Guid.NewGuid(),
            valueId ?? Guid.NewGuid(),
            value,
            displayValue,
            colorCode,
            swatchUrl,
            sortOrder);
    }

    private static Product CreateTestProduct(
        string name = "Test Product",
        string slug = "test-product")
    {
        return Product.Create(name, slug, 99.99m, "VND", TestTenantId);
    }

    private static Product CreateTestProductWithOptionValues()
    {
        var product = CreateTestProduct();
        var option = product.AddOption("color", "Color");
        option.AddValue("red", "Red");
        option.AddValue("blue", "Blue");
        return product;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidData_ShouldUpdateOptionValue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOptionValues();
        var optionId = existingProduct.Options.First().Id;
        var valueId = existingProduct.Options.First().Values.First().Id;

        var command = CreateTestCommand(
            productId: productId,
            optionId: optionId,
            valueId: valueId,
            value: "green",
            displayValue: "Green",
            colorCode: "#00FF00",
            swatchUrl: "https://example.com/green-swatch.jpg",
            sortOrder: 5);

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
        result.Value.Should().NotBeNull();
        result.Value.Value.Should().Be("green");
        result.Value.DisplayValue.Should().Be("Green");
        result.Value.ColorCode.Should().Be("#00FF00");
        result.Value.SwatchUrl.Should().Be("https://example.com/green-swatch.jpg");
        result.Value.SortOrder.Should().Be(5);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullDisplayValue_ShouldDefaultToValue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOptionValues();
        var optionId = existingProduct.Options.First().Id;
        var valueId = existingProduct.Options.First().Values.First().Id;

        var command = CreateTestCommand(
            productId: productId,
            optionId: optionId,
            valueId: valueId,
            value: "green",
            displayValue: null);

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
        // When displayValue is null, domain entity defaults to the value field
        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayValue.Should().Be("green");
    }

    [Fact]
    public async Task Handle_WithColorCode_ShouldSetColorCode()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOptionValues();
        var optionId = existingProduct.Options.First().Id;
        var valueId = existingProduct.Options.First().Values.First().Id;

        var command = CreateTestCommand(
            productId: productId,
            optionId: optionId,
            valueId: valueId,
            colorCode: "#FF5733");

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
        result.Value.ColorCode.Should().Be("#FF5733");
    }

    [Fact]
    public async Task Handle_WithSwatchUrl_ShouldSetSwatchUrl()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOptionValues();
        var optionId = existingProduct.Options.First().Id;
        var valueId = existingProduct.Options.First().Values.First().Id;

        var command = CreateTestCommand(
            productId: productId,
            optionId: optionId,
            valueId: valueId,
            swatchUrl: "https://cdn.example.com/swatches/red.png");

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
        result.Value.SwatchUrl.Should().Be("https://cdn.example.com/swatches/red.png");
    }

    [Fact]
    public async Task Handle_UpdatingSecondValue_ShouldNotAffectFirstValue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOptionValues();
        var optionId = existingProduct.Options.First().Id;
        var firstValueId = existingProduct.Options.First().Values.First().Id;
        var secondValueId = existingProduct.Options.First().Values.ToList()[1].Id;

        var command = CreateTestCommand(
            productId: productId,
            optionId: optionId,
            valueId: secondValueId,
            value: "navy",
            displayValue: "Navy Blue");

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
        // First value should be unchanged
        var firstValue = existingProduct.Options.First().Values.First(v => v.Id == firstValueId);
        firstValue.Value.Should().Be("red");
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
        var existingProduct = CreateTestProductWithOptionValues();
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

    [Fact]
    public async Task Handle_WhenValueNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOptionValues();
        var optionId = existingProduct.Options.First().Id;
        var nonExistentValueId = Guid.NewGuid();

        var command = CreateTestCommand(
            productId: productId,
            optionId: optionId,
            valueId: nonExistentValueId);

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
        result.Error.Code.Should().Be("NOIR-PRODUCT-053");
        result.Error.Message.Should().Contain("value");
        result.Error.Message.Should().Contain("not found");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenValueAlreadyExists_ShouldReturnValidationError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOptionValues();
        var optionId = existingProduct.Options.First().Id;
        var valueId = existingProduct.Options.First().Values.First().Id; // "red" value

        // Try to rename to "Blue" which already exists
        var command = CreateTestCommand(
            productId: productId,
            optionId: optionId,
            valueId: valueId,
            value: "Blue",
            displayValue: "Blue");

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
    public async Task Handle_RenamingToSameValue_ShouldSucceed()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOptionValues();
        var optionId = existingProduct.Options.First().Id;
        var valueId = existingProduct.Options.First().Values.First().Id;
        var currentValue = existingProduct.Options.First().Values.First().Value;

        var command = CreateTestCommand(
            productId: productId,
            optionId: optionId,
            valueId: valueId,
            value: currentValue, // Same value
            displayValue: "Updated Display");

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
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOptionValues();
        var optionId = existingProduct.Options.First().Id;
        var valueId = existingProduct.Options.First().Values.First().Id;
        var command = CreateTestCommand(productId: productId, optionId: optionId, valueId: valueId);
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
    public async Task Handle_WithNullColorCodeAndSwatchUrl_ShouldClearBoth()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOptionValues();
        var optionId = existingProduct.Options.First().Id;
        var valueId = existingProduct.Options.First().Values.First().Id;

        var command = CreateTestCommand(
            productId: productId,
            optionId: optionId,
            valueId: valueId,
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
    public async Task Handle_WithNegativeSortOrder_ShouldSetNegativeSortOrder()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOptionValues();
        var optionId = existingProduct.Options.First().Id;
        var valueId = existingProduct.Options.First().Values.First().Id;

        var command = CreateTestCommand(
            productId: productId,
            optionId: optionId,
            valueId: valueId,
            sortOrder: -1);

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
        result.Value.SortOrder.Should().Be(-1);
    }

    [Fact]
    public async Task Handle_WithSingleValue_ShouldUpdateSuccessfully()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var option = existingProduct.AddOption("size", "Size");
        option.AddValue("small", "Small");
        var optionId = option.Id;
        var valueId = option.Values.Single().Id;

        var command = CreateTestCommand(
            productId: productId,
            optionId: optionId,
            valueId: valueId,
            value: "medium",
            displayValue: "Medium");

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
        result.Value.Value.Should().Be("medium");
    }

    #endregion
}
