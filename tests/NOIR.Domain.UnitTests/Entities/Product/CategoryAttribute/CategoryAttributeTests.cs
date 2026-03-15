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
        catAttr.ShouldNotBeNull();
        catAttr.Id.ShouldNotBe(Guid.Empty);
        catAttr.CategoryId.ShouldBe(TestCategoryId);
        catAttr.AttributeId.ShouldBe(TestAttributeId);
        catAttr.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        // Act
        var catAttr = CreateTestCategoryAttribute();

        // Assert
        catAttr.IsRequired.ShouldBeFalse();
        catAttr.SortOrder.ShouldBe(0);
    }

    [Fact]
    public void Create_WithIsRequiredTrue_ShouldSetRequiredFlag()
    {
        // Act
        var catAttr = CreateTestCategoryAttribute(isRequired: true);

        // Assert
        catAttr.IsRequired.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithSortOrder_ShouldSetSortOrder()
    {
        // Act
        var catAttr = CreateTestCategoryAttribute(sortOrder: 5);

        // Assert
        catAttr.SortOrder.ShouldBe(5);
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var catAttr = CreateTestCategoryAttribute(tenantId: null);

        // Assert
        catAttr.TenantId.ShouldBeNull();
    }

    [Fact]
    public void Create_MultipleCategoryAttributes_ShouldHaveUniqueIds()
    {
        // Act
        var catAttr1 = CreateTestCategoryAttribute();
        var catAttr2 = CreateTestCategoryAttribute();

        // Assert
        catAttr1.Id.ShouldNotBe(catAttr2.Id);
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
        catAttr.IsRequired.ShouldBeTrue();
    }

    [Fact]
    public void SetRequired_False_ShouldClearIsRequired()
    {
        // Arrange
        var catAttr = CreateTestCategoryAttribute(isRequired: true);

        // Act
        catAttr.SetRequired(false);

        // Assert
        catAttr.IsRequired.ShouldBeFalse();
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
        catAttr.SortOrder.ShouldBe(10);
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
        catAttr.SortOrder.ShouldBe(sortOrder);
    }

    #endregion
}
