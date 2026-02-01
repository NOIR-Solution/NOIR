using NOIR.Application.Features.ProductAttributes.Commands.AssignCategoryAttribute;
using NOIR.Application.UnitTests.Common;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Commands.AssignCategoryAttribute;

/// <summary>
/// Unit tests for AssignCategoryAttributeCommandHandler.
/// Tests assigning product attributes to categories.
/// </summary>
public class AssignCategoryAttributeCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IRepository<ProductCategory, Guid>> _categoryRepositoryMock;
    private readonly Mock<IRepository<ProductAttribute, Guid>> _attributeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly AssignCategoryAttributeCommandHandler _handler;

    public AssignCategoryAttributeCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _categoryRepositoryMock = new Mock<IRepository<ProductCategory, Guid>>();
        _attributeRepositoryMock = new Mock<IRepository<ProductAttribute, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new AssignCategoryAttributeCommandHandler(
            _dbContextMock.Object,
            _categoryRepositoryMock.Object,
            _attributeRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static ProductCategory CreateTestCategory(
        string name = "Test Category",
        string slug = "test-category",
        string? tenantId = "tenant-1")
    {
        return ProductCategory.Create(name, slug, tenantId: tenantId);
    }

    private static ProductAttribute CreateTestAttribute(
        string code = "test_attr",
        string name = "Test Attribute",
        AttributeType type = AttributeType.Text,
        string? tenantId = "tenant-1")
    {
        return ProductAttribute.Create(code, name, type, tenantId);
    }

    private static CategoryAttribute CreateCategoryAttributeWithNavigation(
        ProductCategory category,
        ProductAttribute attribute,
        bool isRequired = false,
        int sortOrder = 0)
    {
        var ca = CategoryAttribute.Create(category.Id, attribute.Id, isRequired, sortOrder, category.TenantId);
        typeof(CategoryAttribute).GetProperty("Category")!.SetValue(ca, category);
        typeof(CategoryAttribute).GetProperty("Attribute")!.SetValue(ca, attribute);
        return ca;
    }

    /// <summary>
    /// Sets up the CategoryAttributes mock to handle both the existence check and the re-fetch after creation.
    /// </summary>
    private void SetupCategoryAttributesMockForCreate(
        ProductCategory category,
        ProductAttribute attribute,
        bool isRequired = false,
        int sortOrder = 0)
    {
        CategoryAttribute? addedItem = null;

        var mockDbSet = new Mock<DbSet<CategoryAttribute>>();

        // Setup Add to capture the added item and populate navigation properties
        mockDbSet.Setup(m => m.Add(It.IsAny<CategoryAttribute>()))
            .Callback<CategoryAttribute>(item =>
            {
                // Simulate what EF would do - populate navigation properties
                typeof(CategoryAttribute).GetProperty("Category")!.SetValue(item, category);
                typeof(CategoryAttribute).GetProperty("Attribute")!.SetValue(item, attribute);
                addedItem = item;
            });

        // Setup the queryable to return the added item after Add is called
        Func<IQueryable<CategoryAttribute>> getQueryable = () =>
        {
            if (addedItem != null)
                return new List<CategoryAttribute> { addedItem }.AsQueryable();
            return new List<CategoryAttribute>().AsQueryable();
        };

        // Use a dynamic provider that checks addedItem state
        mockDbSet.As<IQueryable<CategoryAttribute>>()
            .Setup(m => m.Provider)
            .Returns(() => new TestAsyncQueryProvider<CategoryAttribute>(getQueryable().Provider));
        mockDbSet.As<IQueryable<CategoryAttribute>>()
            .Setup(m => m.Expression)
            .Returns(() => getQueryable().Expression);
        mockDbSet.As<IQueryable<CategoryAttribute>>()
            .Setup(m => m.ElementType)
            .Returns(() => getQueryable().ElementType);
        mockDbSet.As<IQueryable<CategoryAttribute>>()
            .Setup(m => m.GetEnumerator())
            .Returns(() => getQueryable().GetEnumerator());
        mockDbSet.As<IAsyncEnumerable<CategoryAttribute>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(() => new TestAsyncEnumerator<CategoryAttribute>(getQueryable().GetEnumerator()));

        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(mockDbSet.Object);
    }

    /// <summary>
    /// Sets up the CategoryAttributes mock for scenarios where the link already exists.
    /// </summary>
    private void SetupCategoryAttributesMockWithExisting(CategoryAttribute existingItem)
    {
        var list = new List<CategoryAttribute> { existingItem };
        var mockDbSet = list.BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(mockDbSet.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldAssignAttributeAndReturnSuccess()
    {
        // Arrange
        var category = CreateTestCategory("Electronics", "electronics");
        var attribute = CreateTestAttribute("color", "Color");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _attributeRepositoryMock
            .Setup(x => x.GetByIdAsync(attribute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        SetupCategoryAttributesMockForCreate(category, attribute, true, 5);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new AssignCategoryAttributeCommand(
            category.Id,
            attribute.Id,
            IsRequired: true,
            SortOrder: 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.CategoryName.Should().Be("Electronics");
        result.Value.AttributeName.Should().Be("Color");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithOptionalAttribute_ShouldAssignWithCorrectSettings()
    {
        // Arrange
        var category = CreateTestCategory();
        var attribute = CreateTestAttribute();

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _attributeRepositoryMock
            .Setup(x => x.GetByIdAsync(attribute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        SetupCategoryAttributesMockForCreate(category, attribute, false, 10);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new AssignCategoryAttributeCommand(
            category.Id,
            attribute.Id,
            IsRequired: false,
            SortOrder: 10);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldSetNamesForAudit()
    {
        // Arrange
        var category = CreateTestCategory("Electronics", "electronics");
        var attribute = CreateTestAttribute("screen_size", "Screen Size");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _attributeRepositoryMock
            .Setup(x => x.GetByIdAsync(attribute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        SetupCategoryAttributesMockForCreate(category, attribute);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new AssignCategoryAttributeCommand(category.Id, attribute.Id);

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
    public async Task Handle_WhenCategoryNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory?)null);

        var command = new AssignCategoryAttributeCommand(categoryId, attributeId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAttributeNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var category = CreateTestCategory();
        var attributeId = Guid.NewGuid();

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _attributeRepositoryMock
            .Setup(x => x.GetByIdAsync(attributeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        var command = new AssignCategoryAttributeCommand(category.Id, attributeId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Attribute.NotFound);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenLinkAlreadyExists_ShouldReturnConflictError()
    {
        // Arrange
        var category = CreateTestCategory();
        var attribute = CreateTestAttribute();

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _attributeRepositoryMock
            .Setup(x => x.GetByIdAsync(attribute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        // Existing link
        var existingLink = CreateCategoryAttributeWithNavigation(category, attribute);
        SetupCategoryAttributesMockWithExisting(existingLink);

        var command = new AssignCategoryAttributeCommand(category.Id, attribute.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Attribute.AlreadyLinkedToCategory);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithDefaultSortOrder_ShouldUseZero()
    {
        // Arrange
        var category = CreateTestCategory();
        var attribute = CreateTestAttribute();

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _attributeRepositoryMock
            .Setup(x => x.GetByIdAsync(attribute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attribute);

        SetupCategoryAttributesMockForCreate(category, attribute, false, 0);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Default command with no sort order specified
        var command = new AssignCategoryAttributeCommand(category.Id, attribute.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SortOrder.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassToAllServices()
    {
        // Arrange
        var category = CreateTestCategory();
        var attribute = CreateTestAttribute();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(category.Id, token))
            .ReturnsAsync(category);

        _attributeRepositoryMock
            .Setup(x => x.GetByIdAsync(attribute.Id, token))
            .ReturnsAsync(attribute);

        SetupCategoryAttributesMockForCreate(category, attribute);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        var command = new AssignCategoryAttributeCommand(category.Id, attribute.Id);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _categoryRepositoryMock.Verify(x => x.GetByIdAsync(category.Id, token), Times.Once);
        _attributeRepositoryMock.Verify(x => x.GetByIdAsync(attribute.Id, token), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(token), Times.Once);
    }

    #endregion
}
