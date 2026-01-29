using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Queries.GetProductOptionValueById;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products;

/// <summary>
/// Unit tests for GetProductOptionValueByIdQueryHandler.
/// Tests product option value retrieval by value ID for before-state resolution in audit logging.
/// </summary>
public class GetProductOptionValueByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly GetProductOptionValueByIdQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetProductOptionValueByIdQueryHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _handler = new GetProductOptionValueByIdQueryHandler(_productRepositoryMock.Object);
    }

    private static Product CreateTestProductWithOptionValue(
        string optionName = "Color",
        string valueName = "Red",
        string? displayValue = "Red")
    {
        var product = Product.Create("Test Product", "test-product", 99.99m, "VND", TestTenantId);
        var option = product.AddOption(optionName, optionName);
        option.AddValue(valueName, displayValue);
        return product;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidValueId_ShouldReturnProductOptionValue()
    {
        // Arrange
        var product = CreateTestProductWithOptionValue();
        var optionValue = product.Options.First().Values.First();
        var query = new GetProductOptionValueByIdQuery(optionValue.Id);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByOptionValueIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("red");
        result.Value.DisplayValue.Should().Be("Red");
    }

    [Fact]
    public async Task Handle_WithColorCodeAndSwatchUrl_ShouldIncludeAll()
    {
        // Arrange
        var product = CreateTestProductWithOptionValue();
        var optionValue = product.Options.First().Values.First();
        optionValue.SetColorCode("#FF0000");
        optionValue.SetSwatchUrl("https://example.com/red.png");
        var query = new GetProductOptionValueByIdQuery(optionValue.Id);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByOptionValueIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ColorCode.Should().Be("#FF0000");
        result.Value.SwatchUrl.Should().Be("https://example.com/red.png");
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetProductOptionValueByIdQuery(Guid.NewGuid());

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByOptionValueIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-PRODUCT-052");
    }

    [Fact]
    public async Task Handle_WhenValueNotFoundInProduct_ShouldReturnNotFound()
    {
        // Arrange
        var product = CreateTestProductWithOptionValue();
        var differentValueId = Guid.NewGuid();
        var query = new GetProductOptionValueByIdQuery(differentValueId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByOptionValueIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-PRODUCT-052");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var product = CreateTestProductWithOptionValue();
        var optionValue = product.Options.First().Values.First();
        var query = new GetProductOptionValueByIdQuery(optionValue.Id);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByOptionValueIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _productRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductByOptionValueIdSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleOptionsAndValues_ShouldFindCorrectValue()
    {
        // Arrange
        var product = Product.Create("Test Product", "test-product", 99.99m, "VND", TestTenantId);
        var colorOption = product.AddOption("color", "Color");
        colorOption.AddValue("red", "Red");
        colorOption.AddValue("blue", "Blue");
        var sizeOption = product.AddOption("size", "Size");
        var smallValue = sizeOption.AddValue("small", "Small");
        sizeOption.AddValue("large", "Large");

        var query = new GetProductOptionValueByIdQuery(smallValue.Id);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByOptionValueIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("small");
        result.Value.DisplayValue.Should().Be("Small");
    }

    [Fact]
    public async Task Handle_ShouldMapAllValueFields()
    {
        // Arrange
        var product = CreateTestProductWithOptionValue("Size", "medium", "Medium");
        var optionValue = product.Options.First().Values.First();
        optionValue.SetColorCode("#00FF00");
        optionValue.SetSwatchUrl("https://example.com/medium.png");
        var query = new GetProductOptionValueByIdQuery(optionValue.Id);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByOptionValueIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Id.Should().Be(optionValue.Id);
        dto.Value.Should().Be("medium");
        dto.DisplayValue.Should().Be("Medium");
        dto.ColorCode.Should().Be("#00FF00");
        dto.SwatchUrl.Should().Be("https://example.com/medium.png");
        dto.SortOrder.Should().Be(0);
    }

    #endregion
}
