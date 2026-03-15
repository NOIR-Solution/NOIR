using NOIR.Application.Features.Products.Commands.DeleteProductOptionValue;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products.Commands.DeleteProductOptionValue;

/// <summary>
/// Unit tests for DeleteProductOptionValueCommandHandler.
/// Tests deleting values from product options with mocked dependencies.
/// </summary>
public class DeleteProductOptionValueCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly DeleteProductOptionValueCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public DeleteProductOptionValueCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeleteProductOptionValueCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static DeleteProductOptionValueCommand CreateTestCommand(
        Guid? productId = null,
        Guid? optionId = null,
        Guid? valueId = null,
        string? valueName = null)
    {
        return new DeleteProductOptionValueCommand(
            productId ?? Guid.NewGuid(),
            optionId ?? Guid.NewGuid(),
            valueId ?? Guid.NewGuid(),
            valueName);
    }

    private static Product CreateTestProduct(
        string name = "Test Product",
        string slug = "test-product")
    {
        return Product.Create(name, slug, 99.99m, "VND", TestTenantId);
    }

    private static Product CreateTestProductWithOptionAndValues()
    {
        var product = CreateTestProduct();
        var option = product.AddOption("Color", "Color");
        option.AddValue("Red", "Red");
        option.AddValue("Blue", "Blue");
        return product;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidValueId_ShouldDeleteValue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOptionAndValues();
        var option = existingProduct.Options.First();
        var valueToDelete = option.Values.First();
        var command = CreateTestCommand(
            productId: productId,
            optionId: option.Id,
            valueId: valueToDelete.Id);

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
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBe(true);
        option.Values.Count().ShouldBe(1);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeleteLastValue_ShouldSucceed()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var option = existingProduct.AddOption("Size", "Size");
        var onlyValue = option.AddValue("Medium", "Medium");

        var command = CreateTestCommand(
            productId: productId,
            optionId: option.Id,
            valueId: onlyValue.Id);

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
        result.IsSuccess.ShouldBe(true);
        option.Values.ShouldBeEmpty();
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
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("NOIR-PRODUCT-021");
        result.Error.Message.ShouldContain("Product");
        result.Error.Message.ShouldContain("not found");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOptionNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOptionAndValues();
        var nonExistentOptionId = Guid.NewGuid();
        var command = CreateTestCommand(
            productId: productId,
            optionId: nonExistentOptionId,
            valueId: Guid.NewGuid());

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForOptionUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("NOIR-PRODUCT-051");
        result.Error.Message.ShouldContain("Option");
        result.Error.Message.ShouldContain("not found");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValueNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOptionAndValues();
        var option = existingProduct.Options.First();
        var nonExistentValueId = Guid.NewGuid();
        var command = CreateTestCommand(
            productId: productId,
            optionId: option.Id,
            valueId: nonExistentValueId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForOptionUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("NOIR-PRODUCT-053");
        result.Error.Message.ShouldContain("Option value");
        result.Error.Message.ShouldContain("not found");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOptionHasNoValues_ShouldReturnNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var option = existingProduct.AddOption("EmptyOption", "Empty Option");
        var someValueId = Guid.NewGuid();

        var command = CreateTestCommand(
            productId: productId,
            optionId: option.Id,
            valueId: someValueId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForOptionUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("NOIR-PRODUCT-053");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOptionAndValues();
        var option = existingProduct.Options.First();
        var valueId = option.Values.First().Id;
        var command = CreateTestCommand(
            productId: productId,
            optionId: option.Id,
            valueId: valueId);
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
    public async Task Handle_AfterDeletion_ShouldMaintainCorrectValueCount()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var option = existingProduct.AddOption("Color", "Color");
        option.AddValue("Red", "Red");
        option.AddValue("Green", "Green");
        option.AddValue("Blue", "Blue");

        var valueToDelete = option.Values.Skip(1).First();
        var command = CreateTestCommand(
            productId: productId,
            optionId: option.Id,
            valueId: valueToDelete.Id);

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
        result.IsSuccess.ShouldBe(true);
        option.Values.Count().ShouldBe(2);
        option.Values.ShouldNotContain(v => v.Id == valueToDelete.Id);
    }

    [Fact]
    public async Task Handle_WithValueName_ShouldReturnSuccessfully()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithOptionAndValues();
        var option = existingProduct.Options.First();
        var valueToDelete = option.Values.First();
        var command = CreateTestCommand(
            productId: productId,
            optionId: option.Id,
            valueId: valueToDelete.Id,
            valueName: "Red");

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
        result.IsSuccess.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_WhenProductHasMultipleOptions_ShouldDeleteFromCorrectOption()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var colorOption = existingProduct.AddOption("Color", "Color");
        colorOption.AddValue("Red", "Red");
        colorOption.AddValue("Blue", "Blue");

        var sizeOption = existingProduct.AddOption("Size", "Size");
        sizeOption.AddValue("Small", "Small");
        sizeOption.AddValue("Large", "Large");

        var colorValueToDelete = colorOption.Values.First();
        var command = CreateTestCommand(
            productId: productId,
            optionId: colorOption.Id,
            valueId: colorValueToDelete.Id);

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
        result.IsSuccess.ShouldBe(true);
        colorOption.Values.Count().ShouldBe(1);
        sizeOption.Values.Count().ShouldBe(2); // Size values should be unchanged
    }

    #endregion
}
