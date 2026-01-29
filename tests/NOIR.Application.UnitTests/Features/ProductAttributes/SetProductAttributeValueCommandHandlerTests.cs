using NOIR.Application.Features.ProductAttributes.Commands.SetProductAttributeValue;
using NOIR.Application.Features.ProductAttributes.DTOs;
using NOIR.Application.Features.ProductAttributes.Specifications;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.ProductAttributes;

/// <summary>
/// Unit tests for SetProductAttributeValueCommandHandler.
/// Tests setting attribute values for products with various attribute types.
/// </summary>
public class SetProductAttributeValueCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IRepository<ProductAttribute, Guid>> _attributeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly SetProductAttributeValueCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public SetProductAttributeValueCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _attributeRepositoryMock = new Mock<IRepository<ProductAttribute, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _messageBusMock = new Mock<IMessageBus>();

        _handler = new SetProductAttributeValueCommandHandler(
            _dbContextMock.Object,
            _productRepositoryMock.Object,
            _attributeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _messageBusMock.Object);
    }

    private static Product CreateTestProduct(string name = "Test Product", string slug = "test-product")
    {
        return Product.Create(name, slug, 99.99m, "VND", TestTenantId);
    }

    private static ProductAttribute CreateTestAttribute(
        string code = "test_attr",
        string name = "Test Attribute",
        AttributeType type = AttributeType.Text)
    {
        return ProductAttribute.Create(code, name, type, TestTenantId);
    }

    private void SetupEmptyAssignments()
    {
        var emptyList = new List<ProductAttributeAssignment>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductAttributeAssignments).Returns(emptyList.Object);
    }

    #endregion

    #region Product Not Found Tests

    [Fact]
    public async Task Handle_WithInvalidProductId_ReturnsNotFoundError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var command = new SetProductAttributeValueCommand(productId, attributeId, null, "test value");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.Product.NotFound);
        result.Error.Message.Should().Contain(productId.ToString());
    }

    #endregion

    #region Attribute Not Found Tests

    [Fact]
    public async Task Handle_WithInvalidAttributeId_ReturnsNotFoundError()
    {
        // Arrange
        var product = CreateTestProduct();
        var attributeId = Guid.NewGuid();

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        var command = new SetProductAttributeValueCommand(product.Id, attributeId, null, "test value");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.Attribute.NotFound);
    }

    #endregion

    #region Text Attribute Tests

    [Fact]
    public async Task Handle_WithTextValue_CreatesAssignmentSuccessfully()
    {
        // Arrange
        var product = CreateTestProduct();
        var attribute = CreateTestAttribute("description", "Description", AttributeType.Text);
        var textValue = "This is a test description";

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        SetupEmptyAssignments();

        var command = new SetProductAttributeValueCommand(product.Id, attribute.Id, null, textValue);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.AttributeCode.Should().Be("description");
        result.Value.AttributeName.Should().Be("Description");
        result.Value.DisplayValue.Should().Be(textValue);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithTextValueExceedingMaxLength_ReturnsValidationError()
    {
        // Arrange
        var product = CreateTestProduct();
        var attribute = CreateTestAttribute("short_text", "Short Text", AttributeType.Text);
        attribute.SetTypeConfiguration(null, null, null, null, 10); // maxLength = 10

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        SetupEmptyAssignments();

        var command = new SetProductAttributeValueCommand(
            product.Id, attribute.Id, null, "This text is way too long for the field");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Message.Should().Contain("maximum length");
    }

    #endregion

    #region Number Attribute Tests

    [Fact]
    public async Task Handle_WithNumberValue_CreatesAssignmentSuccessfully()
    {
        // Arrange
        var product = CreateTestProduct();
        var attribute = CreateTestAttribute("weight", "Weight", AttributeType.Number);
        attribute.SetTypeConfiguration("kg", null, null, null, null);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        SetupEmptyAssignments();

        var command = new SetProductAttributeValueCommand(product.Id, attribute.Id, null, 5.5m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayValue.Should().Be("5.5 kg");
    }

    [Fact]
    public async Task Handle_WithNumberBelowMinValue_ReturnsValidationError()
    {
        // Arrange
        var product = CreateTestProduct();
        var attribute = CreateTestAttribute("rating", "Rating", AttributeType.Number);
        attribute.SetTypeConfiguration(null, null, 1m, 5m, null); // min=1, max=5

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        SetupEmptyAssignments();

        var command = new SetProductAttributeValueCommand(product.Id, attribute.Id, null, 0m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Message.Should().Contain("at least");
    }

    #endregion

    #region Boolean Attribute Tests

    [Fact]
    public async Task Handle_WithBooleanValue_CreatesAssignmentSuccessfully()
    {
        // Arrange
        var product = CreateTestProduct();
        var attribute = CreateTestAttribute("is_featured", "Is Featured", AttributeType.Boolean);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        SetupEmptyAssignments();

        var command = new SetProductAttributeValueCommand(product.Id, attribute.Id, null, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayValue.Should().Be("Yes");
    }

    #endregion

    #region Select Attribute Tests

    [Fact]
    public async Task Handle_WithSelectValue_CreatesAssignmentSuccessfully()
    {
        // Arrange
        var product = CreateTestProduct();
        var attribute = CreateTestAttribute("color", "Color", AttributeType.Select);
        var redValue = attribute.AddValue("red", "Red");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        SetupEmptyAssignments();

        var command = new SetProductAttributeValueCommand(product.Id, attribute.Id, null, redValue.Id.ToString());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayValue.Should().Be("Red");
    }

    [Fact]
    public async Task Handle_WithInvalidSelectValueId_ReturnsValidationError()
    {
        // Arrange
        var product = CreateTestProduct();
        var attribute = CreateTestAttribute("color", "Color", AttributeType.Select);
        attribute.AddValue("red", "Red");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        SetupEmptyAssignments();

        var invalidValueId = Guid.NewGuid();
        var command = new SetProductAttributeValueCommand(product.Id, attribute.Id, null, invalidValueId.ToString());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Message.Should().Contain("not found for this attribute");
    }

    #endregion

    #region Color Attribute Tests

    [Fact]
    public async Task Handle_WithColorValue_CreatesAssignmentSuccessfully()
    {
        // Arrange
        var product = CreateTestProduct();
        var attribute = CreateTestAttribute("accent_color", "Accent Color", AttributeType.Color);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        SetupEmptyAssignments();

        var command = new SetProductAttributeValueCommand(product.Id, attribute.Id, null, "#FF5733");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayValue.Should().Be("#FF5733");
    }

    [Fact]
    public async Task Handle_WithInvalidColorFormat_ReturnsValidationError()
    {
        // Arrange
        var product = CreateTestProduct();
        var attribute = CreateTestAttribute("accent_color", "Accent Color", AttributeType.Color);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        SetupEmptyAssignments();

        var command = new SetProductAttributeValueCommand(product.Id, attribute.Id, null, "not-a-color");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Message.Should().Contain("Invalid color format");
    }

    #endregion

    #region Null Value Tests

    [Fact]
    public async Task Handle_WithNullValue_CreatesAssignmentWithEmptyValue()
    {
        // Arrange
        var product = CreateTestProduct();
        var attribute = CreateTestAttribute("optional_note", "Optional Note", AttributeType.Text);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        SetupEmptyAssignments();

        var command = new SetProductAttributeValueCommand(product.Id, attribute.Id, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Variant Tests

    [Fact]
    public async Task Handle_WithInvalidVariantId_ReturnsNotFoundError()
    {
        // Arrange
        var product = CreateTestProduct();
        var attribute = CreateTestAttribute("size", "Size", AttributeType.Text);
        var invalidVariantId = Guid.NewGuid();

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        var command = new SetProductAttributeValueCommand(product.Id, attribute.Id, invalidVariantId, "Large");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.Product.VariantNotFound);
    }

    #endregion
}
