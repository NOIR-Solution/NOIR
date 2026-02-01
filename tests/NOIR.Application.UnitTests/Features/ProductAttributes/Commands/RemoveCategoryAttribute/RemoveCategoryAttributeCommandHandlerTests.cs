using NOIR.Application.Features.ProductAttributes.Commands.RemoveCategoryAttribute;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Commands.RemoveCategoryAttribute;

/// <summary>
/// Unit tests for RemoveCategoryAttributeCommandHandler.
/// Tests removing attributes from categories.
/// </summary>
public class RemoveCategoryAttributeCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RemoveCategoryAttributeCommandHandler _handler;

    public RemoveCategoryAttributeCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new RemoveCategoryAttributeCommandHandler(
            _dbContextMock.Object,
            _unitOfWorkMock.Object);
    }

    private static CategoryAttribute CreateTestCategoryAttribute(
        Guid categoryId,
        Guid attributeId,
        string? categoryName = "Test Category",
        string? attributeName = "Test Attribute",
        string? tenantId = "tenant-1")
    {
        var categoryAttribute = CategoryAttribute.Create(categoryId, attributeId, tenantId: tenantId);

        if (categoryName != null)
        {
            var category = ProductCategory.Create(categoryName, "test-category", tenantId: tenantId);
            typeof(CategoryAttribute).GetProperty("Category")!.SetValue(categoryAttribute, category);
        }

        if (attributeName != null)
        {
            var attribute = ProductAttribute.Create("test_attr", attributeName, AttributeType.Text, tenantId);
            typeof(CategoryAttribute).GetProperty("Attribute")!.SetValue(categoryAttribute, attribute);
        }

        return categoryAttribute;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidLink_ShouldRemoveAndReturnSuccess()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();
        var categoryAttribute = CreateTestCategoryAttribute(categoryId, attributeId);

        var categoryAttributes = new List<CategoryAttribute> { categoryAttribute }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(categoryAttributes.Object);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RemoveCategoryAttributeCommand(categoryId, attributeId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        categoryAttributes.Verify(x => x.Remove(categoryAttribute), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSetNamesForAudit()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();
        var categoryAttribute = CreateTestCategoryAttribute(
            categoryId,
            attributeId,
            categoryName: "Electronics",
            attributeName: "Screen Size");

        var categoryAttributes = new List<CategoryAttribute> { categoryAttribute }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(categoryAttributes.Object);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RemoveCategoryAttributeCommand(categoryId, attributeId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        command.GetTargetDisplayName().Should().Be("Electronics");
        command.GetActionDescription().Should().Contain("Screen Size");
        command.GetActionDescription().Should().Contain("Electronics");
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenLinkNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();

        var emptyCategoryAttributes = new List<CategoryAttribute>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(emptyCategoryAttributes.Object);

        var command = new RemoveCategoryAttributeCommand(categoryId, attributeId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Attribute.CategoryLinkNotFound);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOnlyDifferentCategoryExists_ShouldReturnNotFoundError()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();
        var differentCategoryId = Guid.NewGuid();

        var differentCategoryAttribute = CreateTestCategoryAttribute(differentCategoryId, attributeId);
        var categoryAttributes = new List<CategoryAttribute> { differentCategoryAttribute }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(categoryAttributes.Object);

        var command = new RemoveCategoryAttributeCommand(categoryId, attributeId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Attribute.CategoryLinkNotFound);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassToAllServices()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();
        var categoryAttribute = CreateTestCategoryAttribute(categoryId, attributeId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        var categoryAttributes = new List<CategoryAttribute> { categoryAttribute }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(categoryAttributes.Object);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        var command = new RemoveCategoryAttributeCommand(categoryId, attributeId);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(token), Times.Once);
    }

    #endregion
}
