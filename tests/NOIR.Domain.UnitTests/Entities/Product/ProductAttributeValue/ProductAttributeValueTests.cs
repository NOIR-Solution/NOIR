using NOIR.Domain.Entities.Product;

namespace NOIR.Domain.UnitTests.Entities.Product.ProductAttributeValue;

/// <summary>
/// Unit tests for the ProductAttributeValue entity.
/// Tests factory methods, value normalization, visual display settings,
/// product count management, and active status toggling.
/// </summary>
public class ProductAttributeValueTests
{
    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestAttributeId = Guid.NewGuid();

    #region Helper Methods

    private static Domain.Entities.Product.ProductAttributeValue CreateTestValue(
        Guid? attributeId = null,
        string value = "red",
        string displayValue = "Red",
        int sortOrder = 0,
        string? tenantId = TestTenantId)
    {
        return Domain.Entities.Product.ProductAttributeValue.Create(
            attributeId ?? TestAttributeId,
            value,
            displayValue,
            sortOrder,
            tenantId);
    }

    #endregion

    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidValue()
    {
        // Act
        var attrValue = CreateTestValue();

        // Assert
        attrValue.ShouldNotBeNull();
        attrValue.Id.ShouldNotBe(Guid.Empty);
        attrValue.AttributeId.ShouldBe(TestAttributeId);
        attrValue.Value.ShouldBe("red");
        attrValue.DisplayValue.ShouldBe("Red");
        attrValue.SortOrder.ShouldBe(0);
        attrValue.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        // Act
        var attrValue = CreateTestValue();

        // Assert
        attrValue.IsActive.ShouldBeTrue();
        attrValue.ProductCount.ShouldBe(0);
        attrValue.ColorCode.ShouldBeNull();
        attrValue.SwatchUrl.ShouldBeNull();
        attrValue.IconUrl.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldNormalizeValue()
    {
        // Act
        var attrValue = Domain.Entities.Product.ProductAttributeValue.Create(
            TestAttributeId, "Sky Blue", "Sky Blue", 0);

        // Assert
        attrValue.Value.ShouldBe("sky_blue");
    }

    [Fact]
    public void Create_ShouldLowercaseValue()
    {
        // Act
        var attrValue = Domain.Entities.Product.ProductAttributeValue.Create(
            TestAttributeId, "RED", "Red", 0);

        // Assert
        attrValue.Value.ShouldBe("red");
    }

    [Fact]
    public void Create_WithSortOrder_ShouldSetSortOrder()
    {
        // Act
        var attrValue = CreateTestValue(sortOrder: 5);

        // Assert
        attrValue.SortOrder.ShouldBe(5);
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var attrValue = CreateTestValue(tenantId: null);

        // Assert
        attrValue.TenantId.ShouldBeNull();
    }

    #endregion

    #region UpdateValue Tests

    [Fact]
    public void UpdateValue_ShouldUpdateValueAndDisplayValue()
    {
        // Arrange
        var attrValue = CreateTestValue(value: "red", displayValue: "Red");

        // Act
        attrValue.UpdateValue("blue", "Blue");

        // Assert
        attrValue.Value.ShouldBe("blue");
        attrValue.DisplayValue.ShouldBe("Blue");
    }

    [Fact]
    public void UpdateValue_ShouldNormalizeValue()
    {
        // Arrange
        var attrValue = CreateTestValue();

        // Act
        attrValue.UpdateValue("Dark Blue", "Dark Blue");

        // Assert
        attrValue.Value.ShouldBe("dark_blue");
    }

    [Fact]
    public void UpdateValue_ShouldLowercaseValue()
    {
        // Arrange
        var attrValue = CreateTestValue();

        // Act
        attrValue.UpdateValue("GREEN", "Green");

        // Assert
        attrValue.Value.ShouldBe("green");
    }

    #endregion

    #region SetVisualDisplay Tests

    [Fact]
    public void SetVisualDisplay_ShouldSetAllFields()
    {
        // Arrange
        var attrValue = CreateTestValue();

        // Act
        attrValue.SetVisualDisplay("#FF0000", "https://swatch.png", "https://icon.svg");

        // Assert
        attrValue.ColorCode.ShouldBe("#FF0000");
        attrValue.SwatchUrl.ShouldBe("https://swatch.png");
        attrValue.IconUrl.ShouldBe("https://icon.svg");
    }

    [Fact]
    public void SetVisualDisplay_WithNulls_ShouldClearAll()
    {
        // Arrange
        var attrValue = CreateTestValue();
        attrValue.SetVisualDisplay("#FF0000", "https://swatch.png", "https://icon.svg");

        // Act
        attrValue.SetVisualDisplay(null, null, null);

        // Assert
        attrValue.ColorCode.ShouldBeNull();
        attrValue.SwatchUrl.ShouldBeNull();
        attrValue.IconUrl.ShouldBeNull();
    }

    #endregion

    #region SetSortOrder Tests

    [Fact]
    public void SetSortOrder_ShouldUpdateValue()
    {
        // Arrange
        var attrValue = CreateTestValue();

        // Act
        attrValue.SetSortOrder(10);

        // Assert
        attrValue.SortOrder.ShouldBe(10);
    }

    #endregion

    #region SetActive Tests

    [Fact]
    public void SetActive_False_ShouldDeactivate()
    {
        // Arrange
        var attrValue = CreateTestValue();

        // Act
        attrValue.SetActive(false);

        // Assert
        attrValue.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void SetActive_True_ShouldReactivate()
    {
        // Arrange
        var attrValue = CreateTestValue();
        attrValue.SetActive(false);

        // Act
        attrValue.SetActive(true);

        // Assert
        attrValue.IsActive.ShouldBeTrue();
    }

    #endregion

    #region ProductCount Tests

    [Fact]
    public void UpdateProductCount_ShouldSetCount()
    {
        // Arrange
        var attrValue = CreateTestValue();

        // Act
        attrValue.UpdateProductCount(25);

        // Assert
        attrValue.ProductCount.ShouldBe(25);
    }

    [Fact]
    public void IncrementProductCount_ShouldIncrementByOne()
    {
        // Arrange
        var attrValue = CreateTestValue();

        // Act
        attrValue.IncrementProductCount();
        attrValue.IncrementProductCount();

        // Assert
        attrValue.ProductCount.ShouldBe(2);
    }

    [Fact]
    public void DecrementProductCount_ShouldDecrementByOne()
    {
        // Arrange
        var attrValue = CreateTestValue();
        attrValue.UpdateProductCount(3);

        // Act
        attrValue.DecrementProductCount();

        // Assert
        attrValue.ProductCount.ShouldBe(2);
    }

    [Fact]
    public void DecrementProductCount_AtZero_ShouldNotGoBelowZero()
    {
        // Arrange
        var attrValue = CreateTestValue();
        attrValue.ProductCount.ShouldBe(0);

        // Act
        attrValue.DecrementProductCount();

        // Assert
        attrValue.ProductCount.ShouldBe(0);
    }

    #endregion
}
