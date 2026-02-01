using NOIR.Application.Features.ProductAttributes.Commands.CreateProductAttribute;
using NOIR.Application.Features.ProductAttributes.DTOs;
using NOIR.Application.Features.ProductAttributes.Specifications;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Commands.CreateProductAttribute;

/// <summary>
/// Unit tests for CreateProductAttributeCommandHandler.
/// Tests creating new product attributes.
/// </summary>
public class CreateProductAttributeCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductAttribute, Guid>> _attributeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly CreateProductAttributeCommandHandler _handler;

    public CreateProductAttributeCommandHandlerTests()
    {
        _attributeRepositoryMock = new Mock<IRepository<ProductAttribute, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns("tenant-1");

        _handler = new CreateProductAttributeCommandHandler(
            _attributeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateAttributeAndReturnSuccess()
    {
        // Arrange
        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeCodeExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        _attributeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductAttribute>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateProductAttributeCommand(
            Code: "screen_size",
            Name: "Screen Size",
            Type: "Number",
            IsFilterable: true,
            IsSearchable: true,
            IsRequired: false,
            IsVariantAttribute: false,
            ShowInProductCard: true,
            ShowInSpecifications: true,
            Unit: "inch",
            MinValue: 1,
            MaxValue: 100);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("screen_size");
        result.Value.Name.Should().Be("Screen Size");
        result.Value.Type.Should().Be("Number");
        result.Value.IsFilterable.Should().BeTrue();
        result.Value.Unit.Should().Be("inch");
        _attributeRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ProductAttribute>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Text")]
    [InlineData("Number")]
    [InlineData("Decimal")]
    [InlineData("Select")]
    [InlineData("MultiSelect")]
    [InlineData("Boolean")]
    [InlineData("Date")]
    [InlineData("DateTime")]
    [InlineData("Color")]
    [InlineData("Range")]
    [InlineData("Url")]
    [InlineData("File")]
    [InlineData("TextArea")]
    public async Task Handle_WithValidType_ShouldCreateAttribute(string type)
    {
        // Arrange
        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeCodeExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        _attributeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductAttribute>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateProductAttributeCommand(
            Code: $"attr_{type.ToLower()}",
            Name: $"Test {type}",
            Type: type);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(type);
    }

    [Fact]
    public async Task Handle_WithGlobalAttribute_ShouldSetGlobalFlag()
    {
        // Arrange
        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeCodeExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        _attributeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductAttribute>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateProductAttributeCommand(
            Code: "weight",
            Name: "Weight",
            Type: "Decimal",
            IsGlobal: true,
            Unit: "kg");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsGlobal.Should().BeTrue();
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WithInvalidType_ShouldReturnValidationError()
    {
        // Arrange
        var command = new CreateProductAttributeCommand(
            Code: "test",
            Name: "Test",
            Type: "InvalidType");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Attribute.InvalidValueForType);
        _attributeRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ProductAttribute>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCodeAlreadyExists_ShouldReturnConflictError()
    {
        // Arrange
        var existingAttribute = ProductAttribute.Create("screen_size", "Screen Size", AttributeType.Number, "tenant-1");

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeCodeExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAttribute);

        var command = new CreateProductAttributeCommand(
            Code: "screen_size",
            Name: "Display Size",
            Type: "Number");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Attribute.DuplicateCode);
        _attributeRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ProductAttribute>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassToAllServices()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeCodeExistsSpec>(),
                token))
            .ReturnsAsync((ProductAttribute?)null);

        _attributeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductAttribute>(), token))
            .ReturnsAsync((ProductAttribute entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        var command = new CreateProductAttributeCommand(
            Code: "test",
            Name: "Test",
            Type: "Text");

        // Act
        await _handler.Handle(command, token);

        // Assert
        _attributeRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductAttributeCodeExistsSpec>(), token),
            Times.Once);
        _attributeRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ProductAttribute>(), token), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public async Task Handle_CodeWithSpaces_ShouldNormalizeToUnderscores()
    {
        // Arrange
        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeCodeExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        _attributeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductAttribute>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateProductAttributeCommand(
            Code: "Screen Size",
            Name: "Screen Size",
            Type: "Number");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("screen_size");
    }

    #endregion
}
