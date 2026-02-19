using NOIR.Application.Features.ProductAttributes;
using NOIR.Application.Features.ProductAttributes.DTOs;
using NOIR.Application.Features.ProductAttributes.Queries.GetProductAttributeValueById;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Queries.GetProductAttributeValueById;

/// <summary>
/// Unit tests for GetProductAttributeValueByIdQueryHandler.
/// Tests retrieving a product attribute value by its ID.
/// NOTE: Navigation property (Attribute) is pre-populated via reflection in test helpers
/// because MockQueryable.Moq does not execute EF Include() chains. The DTO mapping
/// from the navigation property is verified, but the Include() call itself is not.
/// Integration tests cover the full query path.
/// </summary>
public class GetProductAttributeValueByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly GetProductAttributeValueByIdQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetProductAttributeValueByIdQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _handler = new GetProductAttributeValueByIdQueryHandler(_dbContextMock.Object);
    }

    private static ProductAttributeValue CreateTestAttributeValue(
        Guid? attributeId = null,
        string value = "red",
        string displayValue = "Red",
        string? colorCode = null,
        string? swatchUrl = null,
        string? iconUrl = null,
        int sortOrder = 0,
        string attributeName = "Color",
        string attributeCode = "color")
    {
        var attrId = attributeId ?? Guid.NewGuid();
        var attrValue = ProductAttributeValue.Create(attrId, value, displayValue, sortOrder, TestTenantId);

        if (colorCode != null || swatchUrl != null || iconUrl != null)
        {
            attrValue.SetVisualDisplay(colorCode, swatchUrl, iconUrl);
        }

        // Set navigation property via reflection (private setter)
        var attribute = ProductAttribute.Create(attributeCode, attributeName, AttributeType.Select, TestTenantId);
        typeof(ProductAttribute).GetProperty("Id")!.SetValue(attribute, attrId);
        typeof(ProductAttributeValue).GetProperty("Attribute")!.SetValue(attrValue, attribute);

        return attrValue;
    }

    private void SetupDbSet(List<ProductAttributeValue> items)
    {
        var mockDbSet = items.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductAttributeValues).Returns(mockDbSet.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_ValueExists_ReturnsSuccess()
    {
        // Arrange
        var attrValue = CreateTestAttributeValue();
        SetupDbSet(new List<ProductAttributeValue> { attrValue });

        var query = new GetProductAttributeValueByIdQuery(attrValue.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ValueExists_ReturnsMappedDto()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attrValue = CreateTestAttributeValue(
            attributeId: attributeId,
            value: "blue",
            displayValue: "Blue",
            colorCode: "#0000FF",
            sortOrder: 3);

        SetupDbSet(new List<ProductAttributeValue> { attrValue });

        var query = new GetProductAttributeValueByIdQuery(attrValue.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Id.Should().Be(attrValue.Id);
        dto.Value.Should().Be("blue");
        dto.DisplayValue.Should().Be("Blue");
        dto.ColorCode.Should().Be("#0000FF");
        dto.SortOrder.Should().Be(3);
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValueWithSwatchUrl_ReturnsDtoWithSwatchUrl()
    {
        // Arrange
        var attrValue = CreateTestAttributeValue(
            value: "pattern_a",
            displayValue: "Pattern A",
            swatchUrl: "https://example.com/swatch.png");

        SetupDbSet(new List<ProductAttributeValue> { attrValue });

        var query = new GetProductAttributeValueByIdQuery(attrValue.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SwatchUrl.Should().Be("https://example.com/swatch.png");
    }

    [Fact]
    public async Task Handle_ValueWithIconUrl_ReturnsDtoWithIconUrl()
    {
        // Arrange
        var attrValue = CreateTestAttributeValue(
            value: "size_xl",
            displayValue: "XL",
            iconUrl: "https://example.com/icon.svg");

        SetupDbSet(new List<ProductAttributeValue> { attrValue });

        var query = new GetProductAttributeValueByIdQuery(attrValue.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IconUrl.Should().Be("https://example.com/icon.svg");
    }

    [Fact]
    public async Task Handle_WithMultipleRecords_ReturnsCorrectOne()
    {
        // Arrange
        var target = CreateTestAttributeValue(value: "red", displayValue: "Red");
        var other = CreateTestAttributeValue(value: "green", displayValue: "Green");

        SetupDbSet(new List<ProductAttributeValue> { target, other });

        var query = new GetProductAttributeValueByIdQuery(target.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(target.Id);
    }

    [Fact]
    public async Task Handle_ValueWithNoVisualDisplay_ReturnsNullFields()
    {
        // Arrange
        var attrValue = CreateTestAttributeValue(
            value: "64gb",
            displayValue: "64 GB",
            attributeName: "Storage",
            attributeCode: "storage");

        SetupDbSet(new List<ProductAttributeValue> { attrValue });

        var query = new GetProductAttributeValueByIdQuery(attrValue.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ColorCode.Should().BeNull();
        result.Value.SwatchUrl.Should().BeNull();
        result.Value.IconUrl.Should().BeNull();
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_ValueNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        SetupDbSet(new List<ProductAttributeValue>());

        var query = new GetProductAttributeValueByIdQuery(nonExistentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-ATTR-VALUE-001");
        result.Error.Message.Should().Contain(nonExistentId.ToString());
    }

    [Fact]
    public async Task Handle_WithEmptyGuid_ReturnsNotFoundError()
    {
        // Arrange
        SetupDbSet(new List<ProductAttributeValue>());

        var query = new GetProductAttributeValueByIdQuery(Guid.Empty);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-ATTR-VALUE-001");
    }

    [Fact]
    public async Task Handle_WhenIdDoesNotMatchAnyRecord_ReturnsNotFoundWithCorrectMessage()
    {
        // Arrange
        var existingValue = CreateTestAttributeValue();
        SetupDbSet(new List<ProductAttributeValue> { existingValue });

        var differentId = Guid.NewGuid();
        var query = new GetProductAttributeValueByIdQuery(differentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain(differentId.ToString());
        result.Error.Message.Should().Contain("not found");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldNotThrowWhenNotCancelled()
    {
        // Arrange
        var attrValue = CreateTestAttributeValue();
        SetupDbSet(new List<ProductAttributeValue> { attrValue });

        var cts = new CancellationTokenSource();
        var token = cts.Token;

        var query = new GetProductAttributeValueByIdQuery(attrValue.Id);

        // Act
        var result = await _handler.Handle(query, token);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValueWithZeroProductCount_ReturnsDtoWithZeroProductCount()
    {
        // Arrange
        var attrValue = CreateTestAttributeValue();
        SetupDbSet(new List<ProductAttributeValue> { attrValue });

        var query = new GetProductAttributeValueByIdQuery(attrValue.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ProductCount.Should().Be(0);
    }

    #endregion
}
