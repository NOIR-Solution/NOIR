using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Queries.GetProductOptionById;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products;

/// <summary>
/// Unit tests for GetProductOptionByIdQueryHandler.
/// Tests product option retrieval by option ID for before-state resolution in audit logging.
/// </summary>
public class GetProductOptionByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly GetProductOptionByIdQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetProductOptionByIdQueryHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _handler = new GetProductOptionByIdQueryHandler(_productRepositoryMock.Object);
    }

    private static Product CreateTestProductWithOption(
        string optionName = "Color",
        string? optionDisplayName = "Color")
    {
        var product = Product.Create("Test Product", "test-product", 99.99m, "VND", TestTenantId);
        product.AddOption(optionName, optionDisplayName);
        return product;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidOptionId_ShouldReturnProductOption()
    {
        // Arrange
        var product = CreateTestProductWithOption();
        var option = product.Options.First();
        var query = new GetProductOptionByIdQuery(option.Id);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByOptionIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("color");
        result.Value.DisplayName.Should().Be("Color");
    }

    [Fact]
    public async Task Handle_WithOptionValues_ShouldIncludeAllValues()
    {
        // Arrange
        var product = CreateTestProductWithOption();
        var option = product.Options.First();
        option.AddValue("red", "Red");
        option.AddValue("blue", "Blue");
        var query = new GetProductOptionByIdQuery(option.Id);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByOptionIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Values.Should().HaveCount(2);
        result.Value.Values.Should().Contain(v => v.DisplayValue == "Red");
        result.Value.Values.Should().Contain(v => v.DisplayValue == "Blue");
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetProductOptionByIdQuery(Guid.NewGuid());

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByOptionIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-PRODUCT-051");
    }

    [Fact]
    public async Task Handle_WhenOptionNotFoundInProduct_ShouldReturnNotFound()
    {
        // Arrange
        var product = CreateTestProductWithOption();
        var differentOptionId = Guid.NewGuid();
        var query = new GetProductOptionByIdQuery(differentOptionId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByOptionIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-PRODUCT-051");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var product = CreateTestProductWithOption();
        var option = product.Options.First();
        var query = new GetProductOptionByIdQuery(option.Id);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByOptionIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _productRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductByOptionIdSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldMapOptionFieldsCorrectly()
    {
        // Arrange
        var product = Product.Create("Test Product", "test-product", 99.99m, "VND", TestTenantId);
        var option = product.AddOption("size", "Size");
        option.AddValue("small", "Small");
        var query = new GetProductOptionByIdQuery(option.Id);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByOptionIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Id.Should().Be(option.Id);
        dto.Name.Should().Be("size");
        dto.DisplayName.Should().Be("Size");
        dto.SortOrder.Should().Be(0); // Auto-calculated by AddOption
        dto.Values.Should().HaveCount(1);
    }

    #endregion
}
