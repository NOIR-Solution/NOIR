using NOIR.Application.Features.ProductAttributes.Commands.UpdateProductAttribute;
using NOIR.Application.Features.ProductAttributes.Specifications;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Commands.UpdateProductAttribute;

/// <summary>
/// Unit tests for UpdateProductAttributeCommandHandler.
/// Tests updating product attributes.
/// </summary>
public class UpdateProductAttributeCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductAttribute, Guid>> _attributeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdateProductAttributeCommandHandler _handler;

    public UpdateProductAttributeCommandHandlerTests()
    {
        _attributeRepositoryMock = new Mock<IRepository<ProductAttribute, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UpdateProductAttributeCommandHandler(
            _attributeRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static ProductAttribute CreateTestAttribute(
        string code = "test_attr",
        string name = "Test Attribute",
        AttributeType type = AttributeType.Text,
        string? tenantId = "tenant-1")
    {
        return ProductAttribute.Create(code, name, type, tenantId);
    }

    private static UpdateProductAttributeCommand CreateUpdateCommand(
        Guid id,
        string code = "updated_code",
        string name = "Updated Attribute") => new(
        Id: id,
        Code: code,
        Name: name,
        IsFilterable: true,
        IsSearchable: true,
        IsRequired: false,
        IsVariantAttribute: false,
        ShowInProductCard: true,
        ShowInSpecifications: true,
        IsGlobal: false,
        Unit: "unit",
        ValidationRegex: null,
        MinValue: null,
        MaxValue: null,
        MaxLength: null,
        DefaultValue: null,
        Placeholder: "Enter value",
        HelpText: "Help text",
        SortOrder: 1,
        IsActive: true);

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateAndReturnSuccess()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute();

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeCodeExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateUpdateCommand(attributeId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("updated_code");
        result.Value.Name.Should().Be("Updated Attribute");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUpdateBehaviorFlags()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute();

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeCodeExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateProductAttributeCommand(
            Id: attributeId,
            Code: "color",
            Name: "Color",
            IsFilterable: true,
            IsSearchable: true,
            IsRequired: true,
            IsVariantAttribute: true,
            ShowInProductCard: true,
            ShowInSpecifications: true,
            IsGlobal: false,
            Unit: null,
            ValidationRegex: null,
            MinValue: null,
            MaxValue: null,
            MaxLength: null,
            DefaultValue: null,
            Placeholder: null,
            HelpText: null,
            SortOrder: 1,
            IsActive: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsFilterable.Should().BeTrue();
        result.Value.IsSearchable.Should().BeTrue();
        result.Value.IsRequired.Should().BeTrue();
        result.Value.IsVariantAttribute.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldUpdateTypeConfiguration()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute(type: AttributeType.Number);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeCodeExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateProductAttributeCommand(
            Id: attributeId,
            Code: "screen_size",
            Name: "Screen Size",
            IsFilterable: true,
            IsSearchable: false,
            IsRequired: false,
            IsVariantAttribute: false,
            ShowInProductCard: true,
            ShowInSpecifications: true,
            IsGlobal: false,
            Unit: "inch",
            ValidationRegex: null,
            MinValue: 1,
            MaxValue: 100,
            MaxLength: null,
            DefaultValue: "15",
            Placeholder: "Enter screen size",
            HelpText: "Size in inches",
            SortOrder: 1,
            IsActive: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Unit.Should().Be("inch");
        result.Value.MinValue.Should().Be(1);
        result.Value.MaxValue.Should().Be(100);
    }

    [Fact]
    public async Task Handle_ShouldSetGlobalFlag()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute();

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeCodeExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateProductAttributeCommand(
            Id: attributeId,
            Code: "weight",
            Name: "Weight",
            IsFilterable: true,
            IsSearchable: false,
            IsRequired: false,
            IsVariantAttribute: false,
            ShowInProductCard: true,
            ShowInSpecifications: true,
            IsGlobal: true,
            Unit: "kg",
            ValidationRegex: null,
            MinValue: null,
            MaxValue: null,
            MaxLength: null,
            DefaultValue: null,
            Placeholder: null,
            HelpText: null,
            SortOrder: 1,
            IsActive: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
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
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        var command = CreateUpdateCommand(attributeId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Attribute.NotFound);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCodeAlreadyExists_ShouldReturnConflictError()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute(code: "original_code");
        var existingAttribute = CreateTestAttribute(code: "duplicate_code", name: "Other Attribute");

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeCodeExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAttribute);

        var command = new UpdateProductAttributeCommand(
            Id: attributeId,
            Code: "duplicate_code",
            Name: "Updated",
            IsFilterable: false,
            IsSearchable: false,
            IsRequired: false,
            IsVariantAttribute: false,
            ShowInProductCard: false,
            ShowInSpecifications: false,
            IsGlobal: false,
            Unit: null,
            ValidationRegex: null,
            MinValue: null,
            MaxValue: null,
            MaxLength: null,
            DefaultValue: null,
            Placeholder: null,
            HelpText: null,
            SortOrder: 0,
            IsActive: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Attribute.DuplicateCode);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassToAllServices()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                token))
            .ReturnsAsync(attribute);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeCodeExistsSpec>(),
                token))
            .ReturnsAsync((ProductAttribute?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        var command = CreateUpdateCommand(attributeId);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _attributeRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductAttributeByIdForUpdateSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdatingToSameCode_ShouldSucceed()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute(code: "same_code", name: "Original Name");

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeCodeExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateProductAttributeCommand(
            Id: attributeId,
            Code: "same_code",
            Name: "Updated Name",
            IsFilterable: false,
            IsSearchable: false,
            IsRequired: false,
            IsVariantAttribute: false,
            ShowInProductCard: false,
            ShowInSpecifications: false,
            IsGlobal: false,
            Unit: null,
            ValidationRegex: null,
            MinValue: null,
            MaxValue: null,
            MaxLength: null,
            DefaultValue: null,
            Placeholder: null,
            HelpText: null,
            SortOrder: 0,
            IsActive: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Updated Name");
    }

    #endregion
}
