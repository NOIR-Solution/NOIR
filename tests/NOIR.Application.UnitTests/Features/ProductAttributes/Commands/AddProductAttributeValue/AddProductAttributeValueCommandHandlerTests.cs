using NOIR.Application.Features.ProductAttributes.Commands.AddProductAttributeValue;
using NOIR.Application.Features.ProductAttributes.DTOs;
using NOIR.Application.Features.ProductAttributes.Specifications;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Commands.AddProductAttributeValue;

/// <summary>
/// Unit tests for AddProductAttributeValueCommandHandler.
/// Tests adding values to product attributes.
/// </summary>
public class AddProductAttributeValueCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductAttribute, Guid>> _attributeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly AddProductAttributeValueCommandHandler _handler;

    public AddProductAttributeValueCommandHandlerTests()
    {
        _attributeRepositoryMock = new Mock<IRepository<ProductAttribute, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new AddProductAttributeValueCommandHandler(
            _attributeRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static ProductAttribute CreateTestAttribute(
        Guid? id = null,
        string code = "test_color",
        string name = "Test Color",
        AttributeType type = AttributeType.Select,
        string? tenantId = "tenant-1")
    {
        return ProductAttribute.Create(code, name, type, tenantId);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldAddValueAndReturnSuccess()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute(attributeId, type: AttributeType.Select);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new AddProductAttributeValueCommand(
            attributeId,
            "red",
            "Red",
            ColorCode: "#FF0000",
            SortOrder: 1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("red");
        result.Value.DisplayValue.Should().Be("Red");
        result.Value.ColorCode.Should().Be("#FF0000");
        result.Value.SortOrder.Should().Be(1);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithVisualDisplay_ShouldSetVisualProperties()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute(type: AttributeType.Select);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new AddProductAttributeValueCommand(
            attributeId,
            "blue",
            "Blue",
            ColorCode: "#0000FF",
            SwatchUrl: "/images/blue-swatch.png",
            IconUrl: "/icons/blue.svg",
            SortOrder: 2);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ColorCode.Should().Be("#0000FF");
        result.Value.SwatchUrl.Should().Be("/images/blue-swatch.png");
        result.Value.IconUrl.Should().Be("/icons/blue.svg");
    }

    [Fact]
    public async Task Handle_ForMultiSelectAttribute_ShouldSucceed()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute(type: AttributeType.MultiSelect);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new AddProductAttributeValueCommand(
            attributeId,
            "option1",
            "Option 1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("option1");
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
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        var command = new AddProductAttributeValueCommand(
            attributeId,
            "value",
            "Display Value");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Attribute.NotFound);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ForNonSelectAttribute_ShouldReturnValidationError()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute(type: AttributeType.Text);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        var command = new AddProductAttributeValueCommand(
            attributeId,
            "value",
            "Display Value");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDuplicateValue_ShouldReturnValidationError()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute(type: AttributeType.Select);
        // Add initial value
        attribute.AddValue("red", "Red");

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        var command = new AddProductAttributeValueCommand(
            attributeId,
            "red", // Duplicate value
            "Red Color");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassToRepository()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute(type: AttributeType.Select);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                token))
            .ReturnsAsync(attribute);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        var command = new AddProductAttributeValueCommand(
            attributeId,
            "value",
            "Display Value");

        // Act
        await _handler.Handle(command, token);

        // Assert
        _attributeRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductAttributeByIdForUpdateSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoVisualDisplay_ShouldSucceed()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute(type: AttributeType.Select);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new AddProductAttributeValueCommand(
            attributeId,
            "plain",
            "Plain Value");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ColorCode.Should().BeNull();
        result.Value.SwatchUrl.Should().BeNull();
        result.Value.IconUrl.Should().BeNull();
    }

    #endregion
}
