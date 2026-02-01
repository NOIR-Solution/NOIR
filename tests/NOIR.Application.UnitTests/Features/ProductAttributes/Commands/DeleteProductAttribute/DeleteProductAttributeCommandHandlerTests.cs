using NOIR.Application.Features.ProductAttributes.Commands.DeleteProductAttribute;
using NOIR.Application.Features.ProductAttributes.Specifications;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Commands.DeleteProductAttribute;

/// <summary>
/// Unit tests for DeleteProductAttributeCommandHandler.
/// Tests deleting product attributes with FK constraint validation.
/// </summary>
public class DeleteProductAttributeCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductAttribute, Guid>> _attributeRepositoryMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly DeleteProductAttributeCommandHandler _handler;

    public DeleteProductAttributeCommandHandlerTests()
    {
        _attributeRepositoryMock = new Mock<IRepository<ProductAttribute, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeleteProductAttributeCommandHandler(
            _attributeRepositoryMock.Object,
            _dbContextMock.Object,
            _unitOfWorkMock.Object);
    }

    private static ProductAttribute CreateTestAttribute(
        Guid? id = null,
        string code = "test_attr",
        string name = "Test Attribute",
        AttributeType type = AttributeType.Text,
        string? tenantId = "tenant-1")
    {
        return ProductAttribute.Create(code, name, type, tenantId);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithNoConstraints_ShouldDeleteAndReturnSuccess()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute(attributeId);

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        // No product assignments
        var emptyAssignments = new List<ProductAttributeAssignment>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductAttributeAssignments).Returns(emptyAssignments.Object);

        // No category links
        var emptyCategoryLinks = new List<CategoryAttribute>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(emptyCategoryLinks.Object);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteProductAttributeCommand(attributeId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _attributeRepositoryMock.Verify(x => x.Remove(attribute), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSetAttributeNameForAudit()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute(attributeId, name: "Color");

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        var emptyAssignments = new List<ProductAttributeAssignment>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductAttributeAssignments).Returns(emptyAssignments.Object);

        var emptyCategoryLinks = new List<CategoryAttribute>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(emptyCategoryLinks.Object);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteProductAttributeCommand(attributeId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        command.GetTargetDisplayName().Should().Be("Color");
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

        var command = new DeleteProductAttributeCommand(attributeId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Attribute.NotFound);
        _attributeRepositoryMock.Verify(x => x.Remove(It.IsAny<ProductAttribute>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenHasProductAssignments_ShouldReturnValidationError()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute(attributeId, name: "Size");

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        // Has product assignments
        var assignment = ProductAttributeAssignment.Create(Guid.NewGuid(), attributeId, tenantId: "tenant-1");
        assignment.SetTextValue("value1");
        var assignments = new List<ProductAttributeAssignment> { assignment }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductAttributeAssignments).Returns(assignments.Object);

        var command = new DeleteProductAttributeCommand(attributeId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Attribute.HasProducts);
        result.Error.Message.Should().Contain("Size");
        _attributeRepositoryMock.Verify(x => x.Remove(It.IsAny<ProductAttribute>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenHasCategoryLinks_ShouldReturnValidationError()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute(attributeId, name: "Material");

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        // No product assignments
        var emptyAssignments = new List<ProductAttributeAssignment>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductAttributeAssignments).Returns(emptyAssignments.Object);

        // Has category links
        var category = ProductCategory.Create("Shoes", "shoes", tenantId: "tenant-1");
        var categoryLink = CategoryAttribute.Create(category.Id, attributeId, tenantId: "tenant-1");
        typeof(CategoryAttribute).GetProperty("Category")!.SetValue(categoryLink, category);

        var categoryLinks = new List<CategoryAttribute> { categoryLink }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(categoryLinks.Object);

        var command = new DeleteProductAttributeCommand(attributeId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Attribute.HasCategories);
        result.Error.Message.Should().Contain("Material");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassToAllServices()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var attribute = CreateTestAttribute(attributeId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _attributeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductAttributeByIdForUpdateSpec>(),
                token))
            .ReturnsAsync(attribute);

        var emptyAssignments = new List<ProductAttributeAssignment>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductAttributeAssignments).Returns(emptyAssignments.Object);

        var emptyCategoryLinks = new List<CategoryAttribute>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(emptyCategoryLinks.Object);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        var command = new DeleteProductAttributeCommand(attributeId);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _attributeRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductAttributeByIdForUpdateSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(token), Times.Once);
    }

    #endregion
}
