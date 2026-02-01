using NOIR.Application.Features.ProductAttributes.Commands.RemoveProductAttributeValue;
using NOIR.Application.Features.ProductAttributes.Specifications;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Commands.RemoveProductAttributeValue;

/// <summary>
/// Unit tests for RemoveProductAttributeValueCommandHandler.
/// Tests removing values from product attributes.
/// </summary>
public class RemoveProductAttributeValueCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductAttribute, Guid>> _attributeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RemoveProductAttributeValueCommandHandler _handler;

    public RemoveProductAttributeValueCommandHandlerTests()
    {
        _attributeRepositoryMock = new Mock<IRepository<ProductAttribute, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new RemoveProductAttributeValueCommandHandler(
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
    public async Task Handle_WithValidCommand_ShouldRemoveValueAndReturnSuccess()
    {
        // Arrange
        var attribute = CreateTestAttributeWithValues();
        var valueToRemove = attribute.Values.First(v => v.Value == "red");
        var valueId = valueToRemove.Id;

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RemoveProductAttributeValueCommand(attribute.Id, valueId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        attribute.Values.Should().NotContain(v => v.Value == "red");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSetValueDisplayNameForAudit()
    {
        // Arrange
        var attribute = CreateTestAttributeWithValues();
        var valueToRemove = attribute.Values.First(v => v.Value == "blue");
        var valueId = valueToRemove.Id;

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RemoveProductAttributeValueCommand(attribute.Id, valueId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        command.GetTargetDisplayName().Should().Be("Blue");
    }

    [Fact]
    public async Task Handle_ShouldRemoveOnlySpecifiedValue()
    {
        // Arrange
        var attribute = CreateTestAttributeWithValues();
        var initialCount = attribute.Values.Count;
        var valueToRemove = attribute.Values.First(v => v.Value == "green");
        var valueId = valueToRemove.Id;

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RemoveProductAttributeValueCommand(attribute.Id, valueId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        attribute.Values.Count.Should().Be(initialCount - 1);
        attribute.Values.Should().Contain(v => v.Value == "red");
        attribute.Values.Should().Contain(v => v.Value == "blue");
        attribute.Values.Should().NotContain(v => v.Value == "green");
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

        var command = new RemoveProductAttributeValueCommand(attributeId, valueId);

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

        var command = new RemoveProductAttributeValueCommand(attribute.Id, nonExistentValueId);

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
        var valueToRemove = attribute.Values.First();
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

        var command = new RemoveProductAttributeValueCommand(attribute.Id, valueToRemove.Id);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _attributeRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductAttributeByIdForUpdateSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public async Task Handle_RemovingLastValue_ShouldSucceed()
    {
        // Arrange
        var attribute = ProductAttribute.Create("single", "Single Value", AttributeType.Select, "tenant-1");
        attribute.AddValue("only_option", "Only Option");
        var onlyValue = attribute.Values.First();

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RemoveProductAttributeValueCommand(attribute.Id, onlyValue.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        attribute.Values.Should().BeEmpty();
    }

    #endregion
}
