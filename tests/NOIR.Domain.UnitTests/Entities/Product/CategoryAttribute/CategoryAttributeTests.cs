using NOIR.Domain.Entities.Product;

namespace NOIR.Domain.UnitTests.Entities.Product.CategoryAttribute;

/// <summary>
/// Unit tests for the CategoryAttribute junction entity.
/// Tests factory methods, required flag, and sort order management.
/// </summary>
public class CategoryAttributeTests
{
    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestCategoryId = Guid.NewGuid();
    private static readonly Guid TestAttributeId = Guid.NewGuid();

    #region Helper Methods

    private static Domain.Entities.Product.CategoryAttribute CreateTestCategoryAttribute(
        Guid? categoryId = null,
        Guid? attributeId = null,
        bool isRequired = false,
        int sortOrder = 0,
        string? tenantId = TestTenantId)
    {
        return Domain.Entities.Product.CategoryAttribute.Create(
            categoryId ?? TestCategoryId,
            attributeId ?? TestAttributeId,
            isRequired,
            sortOrder,
            tenantId);
    }

    #endregion

    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidCategoryAttribute()
    {
        // Act
        var catAttr = CreateTestCategoryAttribute();

        // Assert
        catAttr.Should().NotBeNull();
        catAttr.Id.Should().NotBe(Guid.Empty);
        catAttr.CategoryId.Should().Be(TestCategoryId);
        catAttr.AttributeId.Should().Be(TestAttributeId);
        catAttr.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        // Act
        var catAttr = CreateTestCategoryAttribute();

        // Assert
        catAttr.IsRequired.Should().BeFalse();
        catAttr.SortOrder.Should().Be(0);
    }

    [Fact]
    public void Create_WithIsRequiredTrue_ShouldSetRequiredFlag()
    {
        // Act
        var catAttr = CreateTestCategoryAttribute(isRequired: true);

        // Assert
        catAttr.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void Create_WithSortOrder_ShouldSetSortOrder()
    {
        // Act
        var catAttr = CreateTestCategoryAttribute(sortOrder: 5);

        // Assert
        catAttr.SortOrder.Should().Be(5);
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var catAttr = CreateTestCategoryAttribute(tenantId: null);

        // Assert
        catAttr.TenantId.Should().BeNull();
    }

    [Fact]
    public void Create_MultipleCategoryAttributes_ShouldHaveUniqueIds()
    {
        // Act
        var catAttr1 = CreateTestCategoryAttribute();
        var catAttr2 = CreateTestCategoryAttribute();

        // Assert
        catAttr1.Id.Should().NotBe(catAttr2.Id);
    }

    #endregion

    #region SetRequired Tests

    [Fact]
    public void SetRequired_True_ShouldSetIsRequired()
    {
        // Arrange
        var catAttr = CreateTestCategoryAttribute(isRequired: false);

        // Act
        catAttr.SetRequired(true);

        // Assert
        catAttr.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void SetRequired_False_ShouldClearIsRequired()
    {
        // Arrange
        var catAttr = CreateTestCategoryAttribute(isRequired: true);

        // Act
        catAttr.SetRequired(false);

        // Assert
        catAttr.IsRequired.Should().BeFalse();
    }

    #endregion

    #region SetSortOrder Tests

    [Fact]
    public void SetSortOrder_ShouldUpdateValue()
    {
        // Arrange
        var catAttr = CreateTestCategoryAttribute();

        // Act
        catAttr.SetSortOrder(10);

        // Assert
        catAttr.SortOrder.Should().Be(10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void SetSortOrder_VariousValues_ShouldSetCorrectly(int sortOrder)
    {
        // Arrange
        var catAttr = CreateTestCategoryAttribute();

        // Act
        catAttr.SetSortOrder(sortOrder);

        // Assert
        catAttr.SortOrder.Should().Be(sortOrder);
    }

    #endregion
}
