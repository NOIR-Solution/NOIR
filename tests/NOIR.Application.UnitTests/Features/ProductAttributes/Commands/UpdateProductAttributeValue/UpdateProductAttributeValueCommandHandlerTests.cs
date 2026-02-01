using NOIR.Application.Features.ProductAttributes.Commands.UpdateProductAttributeValue;
using NOIR.Application.Features.ProductAttributes.Specifications;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Commands.UpdateProductAttributeValue;

/// <summary>
/// Unit tests for UpdateProductAttributeValueCommandHandler.
/// Tests updating product attribute values.
/// </summary>
public class UpdateProductAttributeValueCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductAttribute, Guid>> _attributeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdateProductAttributeValueCommandHandler _handler;

    public UpdateProductAttributeValueCommandHandlerTests()
    {
        _attributeRepositoryMock = new Mock<IRepository<ProductAttribute, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UpdateProductAttributeValueCommandHandler(
            _attributeRepositoryMock.Object,
            _unitOfWorkMock.Object);
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
    public async Task Handle_WithValidCommand_ShouldUpdateValueAndReturnSuccess()
    {
        // Arrange
        var attribute = CreateTestAttributeWithValues();
        var valueToUpdate = attribute.Values.First(v => v.Value == "red");
        var valueId = valueToUpdate.Id;

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateProductAttributeValueCommand(
            AttributeId: attribute.Id,
            ValueId: valueId,
            Value: "crimson",
            DisplayValue: "Crimson Red",
            ColorCode: "#DC143C",
            SwatchUrl: null,
            IconUrl: null,
            SortOrder: 1,
            IsActive: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("crimson");
        result.Value.DisplayValue.Should().Be("Crimson Red");
        result.Value.ColorCode.Should().Be("#DC143C");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUpdateVisualDisplay()
    {
        // Arrange
        var attribute = CreateTestAttributeWithValues();
        var valueToUpdate = attribute.Values.First(v => v.Value == "blue");
        var valueId = valueToUpdate.Id;

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateProductAttributeValueCommand(
            AttributeId: attribute.Id,
            ValueId: valueId,
            Value: "blue",
            DisplayValue: "Ocean Blue",
            ColorCode: "#0077BE",
            SwatchUrl: "https://example.com/swatch/blue.png",
            IconUrl: "https://example.com/icons/blue.svg",
            SortOrder: 2,
            IsActive: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ColorCode.Should().Be("#0077BE");
        result.Value.SwatchUrl.Should().Be("https://example.com/swatch/blue.png");
        result.Value.IconUrl.Should().Be("https://example.com/icons/blue.svg");
    }

    [Fact]
    public async Task Handle_ShouldUpdateSortOrderAndActiveStatus()
    {
        // Arrange
        var attribute = CreateTestAttributeWithValues();
        var valueToUpdate = attribute.Values.First(v => v.Value == "green");
        var valueId = valueToUpdate.Id;

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateProductAttributeValueCommand(
            AttributeId: attribute.Id,
            ValueId: valueId,
            Value: "green",
            DisplayValue: "Forest Green",
            ColorCode: "#228B22",
            SwatchUrl: null,
            IconUrl: null,
            SortOrder: 99,
            IsActive: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SortOrder.Should().Be(99);
        result.Value.IsActive.Should().BeFalse();
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenAttributeNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var valueId = Guid.NewGuid();

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        var command = new UpdateProductAttributeValueCommand(
            AttributeId: attributeId,
            ValueId: valueId,
            Value: "value",
            DisplayValue: "Display",
            ColorCode: null,
            SwatchUrl: null,
            IconUrl: null,
            SortOrder: 0,
            IsActive: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Attribute.NotFound);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValueNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var attribute = CreateTestAttributeWithValues();
        var nonExistentValueId = Guid.NewGuid();

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        var command = new UpdateProductAttributeValueCommand(
            AttributeId: attribute.Id,
            ValueId: nonExistentValueId,
            Value: "value",
            DisplayValue: "Display",
            ColorCode: null,
            SwatchUrl: null,
            IconUrl: null,
            SortOrder: 0,
            IsActive: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Attribute.ValueNotFound);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassToAllServices()
    {
        // Arrange
        var attribute = CreateTestAttributeWithValues();
        var valueToUpdate = attribute.Values.First();
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

        var command = new UpdateProductAttributeValueCommand(
            AttributeId: attribute.Id,
            ValueId: valueToUpdate.Id,
            Value: "updated",
            DisplayValue: "Updated",
            ColorCode: null,
            SwatchUrl: null,
            IconUrl: null,
            SortOrder: 0,
            IsActive: true);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _attributeRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductAttributeByIdForUpdateSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdatingAllFields_ShouldUpdateCorrectly()
    {
        // Arrange
        var attribute = CreateTestAttributeWithValues();
        var valueToUpdate = attribute.Values.First();

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateProductAttributeValueCommand(
            AttributeId: attribute.Id,
            ValueId: valueToUpdate.Id,
            Value: "new_value",
            DisplayValue: "New Display Value",
            ColorCode: "#FFFFFF",
            SwatchUrl: "https://example.com/swatch.png",
            IconUrl: "https://example.com/icon.svg",
            SortOrder: 50,
            IsActive: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("new_value");
        result.Value.DisplayValue.Should().Be("New Display Value");
        result.Value.ColorCode.Should().Be("#FFFFFF");
        result.Value.SwatchUrl.Should().Be("https://example.com/swatch.png");
        result.Value.IconUrl.Should().Be("https://example.com/icon.svg");
        result.Value.SortOrder.Should().Be(50);
        result.Value.IsActive.Should().BeFalse();
    }

    #endregion
}
