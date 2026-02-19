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
        attrValue.Should().NotBeNull();
        attrValue.Id.Should().NotBe(Guid.Empty);
        attrValue.AttributeId.Should().Be(TestAttributeId);
        attrValue.Value.Should().Be("red");
        attrValue.DisplayValue.Should().Be("Red");
        attrValue.SortOrder.Should().Be(0);
        attrValue.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        // Act
        var attrValue = CreateTestValue();

        // Assert
        attrValue.IsActive.Should().BeTrue();
        attrValue.ProductCount.Should().Be(0);
        attrValue.ColorCode.Should().BeNull();
        attrValue.SwatchUrl.Should().BeNull();
        attrValue.IconUrl.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldNormalizeValue()
    {
        // Act
        var attrValue = Domain.Entities.Product.ProductAttributeValue.Create(
            TestAttributeId, "Sky Blue", "Sky Blue", 0);

        // Assert
        attrValue.Value.Should().Be("sky_blue");
    }

    [Fact]
    public void Create_ShouldLowercaseValue()
    {
        // Act
        var attrValue = Domain.Entities.Product.ProductAttributeValue.Create(
            TestAttributeId, "RED", "Red", 0);

        // Assert
        attrValue.Value.Should().Be("red");
    }

    [Fact]
    public void Create_WithSortOrder_ShouldSetSortOrder()
    {
        // Act
        var attrValue = CreateTestValue(sortOrder: 5);

        // Assert
        attrValue.SortOrder.Should().Be(5);
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var attrValue = CreateTestValue(tenantId: null);

        // Assert
        attrValue.TenantId.Should().BeNull();
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
        attrValue.Value.Should().Be("blue");
        attrValue.DisplayValue.Should().Be("Blue");
    }

    [Fact]
    public void UpdateValue_ShouldNormalizeValue()
    {
        // Arrange
        var attrValue = CreateTestValue();

        // Act
        attrValue.UpdateValue("Dark Blue", "Dark Blue");

        // Assert
        attrValue.Value.Should().Be("dark_blue");
    }

    [Fact]
    public void UpdateValue_ShouldLowercaseValue()
    {
        // Arrange
        var attrValue = CreateTestValue();

        // Act
        attrValue.UpdateValue("GREEN", "Green");

        // Assert
        attrValue.Value.Should().Be("green");
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
        attrValue.ColorCode.Should().Be("#FF0000");
        attrValue.SwatchUrl.Should().Be("https://swatch.png");
        attrValue.IconUrl.Should().Be("https://icon.svg");
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
        attrValue.ColorCode.Should().BeNull();
        attrValue.SwatchUrl.Should().BeNull();
        attrValue.IconUrl.Should().BeNull();
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
        attrValue.SortOrder.Should().Be(10);
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
        attrValue.IsActive.Should().BeFalse();
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
        attrValue.IsActive.Should().BeTrue();
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
        attrValue.ProductCount.Should().Be(25);
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
        attrValue.ProductCount.Should().Be(2);
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
        attrValue.ProductCount.Should().Be(2);
    }

    [Fact]
    public void DecrementProductCount_AtZero_ShouldNotGoBelowZero()
    {
        // Arrange
        var attrValue = CreateTestValue();
        attrValue.ProductCount.Should().Be(0);

        // Act
        attrValue.DecrementProductCount();

        // Assert
        attrValue.ProductCount.Should().Be(0);
    }

    #endregion
}
