using NOIR.Application.Features.ProductAttributes.Queries.GetProductAttributeById;
using NOIR.Application.Features.ProductAttributes.Specifications;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Queries.GetProductAttributeById;

/// <summary>
/// Unit tests for GetProductAttributeByIdQueryHandler.
/// Tests getting a product attribute by its ID.
/// </summary>
public class GetProductAttributeByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductAttribute, Guid>> _attributeRepositoryMock;
    private readonly GetProductAttributeByIdQueryHandler _handler;

    public GetProductAttributeByIdQueryHandlerTests()
    {
        _attributeRepositoryMock = new Mock<IRepository<ProductAttribute, Guid>>();

        _handler = new GetProductAttributeByIdQueryHandler(_attributeRepositoryMock.Object);
    }

    private static ProductAttribute CreateTestAttribute(
        string code = "test_attr",
        string name = "Test Attribute",
        AttributeType type = AttributeType.Text,
        string? tenantId = "tenant-1")
    {
        return ProductAttribute.Create(code, name, type, tenantId);
    }

    private static ProductAttribute CreateTestAttributeWithValues(
        string code = "color",
        string name = "Color",
        string? tenantId = "tenant-1")
    {
        var attribute = ProductAttribute.Create(code, name, AttributeType.Select, tenantId);
        attribute.AddValue("red", "Red");
        attribute.AddValue("blue", "Blue");
        attribute.AddValue("green", "Green");
        return attribute;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnAttribute()
    {
        // Arrange
        var attribute = CreateTestAttribute("screen_size", "Screen Size", AttributeType.Number);
        var attributeId = attribute.Id;

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        var query = new GetProductAttributeByIdQuery(attributeId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("screen_size");
        result.Value.Name.Should().Be("Screen Size");
        result.Value.Type.Should().Be("Number");
    }

    [Fact]
    public async Task Handle_ShouldReturnAttributeWithValues()
    {
        // Arrange
        var attribute = CreateTestAttributeWithValues();
        var attributeId = attribute.Id;

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        var query = new GetProductAttributeByIdQuery(attributeId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Values.Should().HaveCount(3);
        result.Value.Values.Should().Contain(v => v.Value == "red");
        result.Value.Values.Should().Contain(v => v.Value == "blue");
        result.Value.Values.Should().Contain(v => v.Value == "green");
    }

    [Theory]
    [InlineData(AttributeType.Text)]
    [InlineData(AttributeType.Number)]
    [InlineData(AttributeType.Decimal)]
    [InlineData(AttributeType.Boolean)]
    [InlineData(AttributeType.Select)]
    [InlineData(AttributeType.MultiSelect)]
    [InlineData(AttributeType.Date)]
    [InlineData(AttributeType.DateTime)]
    [InlineData(AttributeType.Color)]
    [InlineData(AttributeType.Range)]
    [InlineData(AttributeType.Url)]
    [InlineData(AttributeType.File)]
    [InlineData(AttributeType.TextArea)]
    public async Task Handle_WithDifferentTypes_ShouldReturnCorrectType(AttributeType type)
    {
        // Arrange
        var attribute = CreateTestAttribute($"attr_{type.ToString().ToLower()}", $"Test {type}", type);
        var attributeId = attribute.Id;

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        var query = new GetProductAttributeByIdQuery(attributeId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(type.ToString());
    }

    [Fact]
    public async Task Handle_ShouldReturnAllAttributeProperties()
    {
        // Arrange
        var attribute = CreateTestAttribute("weight", "Weight", AttributeType.Decimal);
        attribute.SetBehaviorFlags(
            isFilterable: true,
            isSearchable: true,
            isRequired: true,
            isVariantAttribute: false);
        attribute.SetDisplayFlags(showInProductCard: true, showInSpecifications: true);
        attribute.SetTypeConfiguration(
            unit: "kg",
            validationRegex: null,
            minValue: 0.1m,
            maxValue: 1000m,
            maxLength: null);
        attribute.SetDefaults(
            defaultValue: "1",
            placeholder: "Enter weight",
            helpText: "Weight in kilograms");
        attribute.SetGlobal(true);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        var query = new GetProductAttributeByIdQuery(attribute.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsFilterable.Should().BeTrue();
        result.Value.IsSearchable.Should().BeTrue();
        result.Value.IsRequired.Should().BeTrue();
        result.Value.ShowInProductCard.Should().BeTrue();
        result.Value.ShowInSpecifications.Should().BeTrue();
        result.Value.Unit.Should().Be("kg");
        result.Value.MinValue.Should().Be(0.1m);
        result.Value.MaxValue.Should().Be(1000m);
        result.Value.IsGlobal.Should().BeTrue();
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenAttributeNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var attributeId = Guid.NewGuid();

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        var query = new GetProductAttributeByIdQuery(attributeId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Attribute.NotFound);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassToRepository()
    {
        // Arrange
        var attribute = CreateTestAttribute();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                token))
            .ReturnsAsync(attribute);

        var query = new GetProductAttributeByIdQuery(attribute.Id);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _attributeRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductAttributeByIdSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyValues_ShouldReturnEmptyValuesList()
    {
        // Arrange
        var attribute = CreateTestAttribute("text_attr", "Text Attribute", AttributeType.Text);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        var query = new GetProductAttributeByIdQuery(attribute.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Values.Should().BeEmpty();
    }

    #endregion
}
