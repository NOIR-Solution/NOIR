using NOIR.Application.Features.ProductAttributes.Commands.BulkUpdateProductAttributes;
using NOIR.Application.Features.ProductAttributes.Commands.SetProductAttributeValue;
using NOIR.Application.Features.ProductAttributes.DTOs;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.ProductAttributes;

/// <summary>
/// Unit tests for BulkUpdateProductAttributesCommandHandler.
/// Tests bulk updating multiple attribute values for a product.
/// </summary>
public class BulkUpdateProductAttributesCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly BulkUpdateProductAttributesCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public BulkUpdateProductAttributesCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _messageBusMock = new Mock<IMessageBus>();

        _handler = new BulkUpdateProductAttributesCommandHandler(
            _productRepositoryMock.Object,
            _messageBusMock.Object);
    }

    private static Product CreateTestProduct(string name = "Test Product", string slug = "test-product")
    {
        return Product.Create(name, slug, 99.99m, "VND", TestTenantId);
    }

    private static ProductAttributeAssignmentDto CreateTestAssignmentDto(
        Guid productId,
        Guid attributeId,
        string value)
    {
        return new ProductAttributeAssignmentDto(
            Guid.NewGuid(),
            productId,
            attributeId,
            "test_code",
            "Test Attribute",
            "Text",
            null,
            value,
            value,
            false);
    }

    #endregion

    #region Product Not Found Tests

    [Fact]
    public async Task Handle_WithInvalidProductId_ReturnsNotFoundError()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var command = new BulkUpdateProductAttributesCommand(
            productId,
            null,
            new List<AttributeValueItem>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.Product.NotFound);
    }

    #endregion

    #region Variant Not Found Tests

    [Fact]
    public async Task Handle_WithInvalidVariantId_ReturnsNotFoundError()
    {
        // Arrange
        var product = CreateTestProduct();
        var invalidVariantId = Guid.NewGuid();

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var command = new BulkUpdateProductAttributesCommand(
            product.Id,
            invalidVariantId,
            new List<AttributeValueItem> { new(Guid.NewGuid(), "Test") });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.Product.VariantNotFound);
    }

    #endregion

    #region Empty Values Tests

    [Fact]
    public async Task Handle_WithNullValues_ReturnsEmptyList()
    {
        // Arrange
        var product = CreateTestProduct();

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var command = new BulkUpdateProductAttributesCommand(
            product.Id,
            null,
            null!);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithEmptyValues_ReturnsEmptyList()
    {
        // Arrange
        var product = CreateTestProduct();

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var command = new BulkUpdateProductAttributesCommand(
            product.Id,
            null,
            new List<AttributeValueItem>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidValues_ReturnsUpdatedAssignments()
    {
        // Arrange
        var product = CreateTestProduct();
        var attributeId1 = Guid.NewGuid();
        var attributeId2 = Guid.NewGuid();

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var assignment1 = CreateTestAssignmentDto(product.Id, attributeId1, "Red");
        var assignment2 = CreateTestAssignmentDto(product.Id, attributeId2, "Large");

        _messageBusMock
            .Setup(x => x.InvokeAsync<Result<ProductAttributeAssignmentDto>>(
                It.Is<SetProductAttributeValueCommand>(c => c.AttributeId == attributeId1),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(Result.Success(assignment1));

        _messageBusMock
            .Setup(x => x.InvokeAsync<Result<ProductAttributeAssignmentDto>>(
                It.Is<SetProductAttributeValueCommand>(c => c.AttributeId == attributeId2),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(Result.Success(assignment2));

        var command = new BulkUpdateProductAttributesCommand(
            product.Id,
            null,
            new List<AttributeValueItem>
            {
                new(attributeId1, "Red"),
                new(attributeId2, "Large")
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        _messageBusMock.Verify(x => x.InvokeAsync<Result<ProductAttributeAssignmentDto>>(
            It.IsAny<SetProductAttributeValueCommand>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<TimeSpan?>()), Times.Exactly(2));
    }

    #endregion

    #region Fail Fast Tests

    [Fact]
    public async Task Handle_FailsFastOnFirstError_DoesNotContinueProcessing()
    {
        // Arrange
        var product = CreateTestProduct();
        var attributeId1 = Guid.NewGuid();
        var attributeId2 = Guid.NewGuid();

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // First attribute fails
        _messageBusMock
            .Setup(x => x.InvokeAsync<Result<ProductAttributeAssignmentDto>>(
                It.Is<SetProductAttributeValueCommand>(c => c.AttributeId == attributeId1),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(Result.Failure<ProductAttributeAssignmentDto>(
                Error.Validation("Value", "Invalid color value", ErrorCodes.Attribute.InvalidValueForType)));

        // Second attribute would succeed, but should never be called
        _messageBusMock
            .Setup(x => x.InvokeAsync<Result<ProductAttributeAssignmentDto>>(
                It.Is<SetProductAttributeValueCommand>(c => c.AttributeId == attributeId2),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(Result.Success(CreateTestAssignmentDto(product.Id, attributeId2, "Large")));

        var command = new BulkUpdateProductAttributesCommand(
            product.Id,
            null,
            new List<AttributeValueItem>
            {
                new(attributeId1, "InvalidColor"),
                new(attributeId2, "Large")
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - should fail fast on first error
        result.IsSuccess.Should().BeFalse();
        result.Error.Message.Should().Contain("Invalid color value");

        // Verify second attribute was never processed (fail-fast behavior)
        _messageBusMock.Verify(x => x.InvokeAsync<Result<ProductAttributeAssignmentDto>>(
            It.Is<SetProductAttributeValueCommand>(c => c.AttributeId == attributeId2),
            It.IsAny<CancellationToken>(),
            It.IsAny<TimeSpan?>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithFirstFailure_ReturnsValidationError()
    {
        // Arrange
        var product = CreateTestProduct();
        var attributeId1 = Guid.NewGuid();
        var attributeId2 = Guid.NewGuid();

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // First attribute fails - second never runs due to fail-fast
        _messageBusMock
            .Setup(x => x.InvokeAsync<Result<ProductAttributeAssignmentDto>>(
                It.Is<SetProductAttributeValueCommand>(c => c.AttributeId == attributeId1),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(Result.Failure<ProductAttributeAssignmentDto>(
                Error.Validation("Value", "Invalid value")));

        var command = new BulkUpdateProductAttributesCommand(
            product.Id,
            null,
            new List<AttributeValueItem>
            {
                new(attributeId1, "Invalid1"),
                new(attributeId2, "Invalid2")
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Message.Should().Contain("Invalid value");
        // With fail-fast, only first attribute is processed
        _messageBusMock.Verify(x => x.InvokeAsync<Result<ProductAttributeAssignmentDto>>(
            It.IsAny<SetProductAttributeValueCommand>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<TimeSpan?>()), Times.Once);
    }

    #endregion

    #region UserId Propagation Tests

    [Fact]
    public async Task Handle_PropagatesUserIdToChildCommands()
    {
        // Arrange
        var product = CreateTestProduct();
        var attributeId = Guid.NewGuid();
        var userId = "test-user-id";

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        SetProductAttributeValueCommand? capturedCommand = null;
        _messageBusMock
            .Setup(x => x.InvokeAsync<Result<ProductAttributeAssignmentDto>>(
                It.IsAny<SetProductAttributeValueCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .Callback<object, CancellationToken, TimeSpan?>((cmd, _, _) =>
                capturedCommand = cmd as SetProductAttributeValueCommand)
            .ReturnsAsync(Result.Success(CreateTestAssignmentDto(product.Id, attributeId, "Value")));

        var command = new BulkUpdateProductAttributesCommand(
            product.Id,
            null,
            new List<AttributeValueItem> { new(attributeId, "Value") })
        {
            UserId = userId
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedCommand.Should().NotBeNull();
        capturedCommand!.UserId.Should().Be(userId);
    }

    #endregion
}
