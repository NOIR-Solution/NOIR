using NOIR.Domain.Entities.Product;

namespace NOIR.Domain.UnitTests.Entities.Product.ProductOptionValue;

/// <summary>
/// Unit tests for the ProductOptionValue entity.
/// Tests creation via parent ProductOption, value normalization,
/// update methods, color code, swatch URL, and sort order.
/// ProductOptionValue.Create is internal, so instances are created via ProductOption.AddValue.
/// </summary>
public class ProductOptionValueTests
{
    private const string TestTenantId = "test-tenant";

    #region Helper Methods

    private static Domain.Entities.Product.Product CreateTestProduct()
    {
        return Domain.Entities.Product.Product.Create("Test Product", "test-product", 100_000m, "VND", TestTenantId);
    }

    private static Domain.Entities.Product.ProductOption CreateTestOption()
    {
        var product = CreateTestProduct();
        return product.AddOption("Color", "Color");
    }

    private static Domain.Entities.Product.ProductOptionValue CreateTestOptionValue(
        string value = "red",
        string? displayValue = "Red")
    {
        var option = CreateTestOption();
        return option.AddValue(value, displayValue);
    }

    #endregion

    #region Creation Tests (via ProductOption.AddValue)

    [Fact]
    public void Create_ViaOption_ShouldSetAllProperties()
    {
        // Arrange
        var option = CreateTestOption();

        // Act
        var value = option.AddValue("red", "Red");

        // Assert
        value.ShouldNotBeNull();
        value.Id.ShouldNotBe(Guid.Empty);
        value.OptionId.ShouldBe(option.Id);
        value.Value.ShouldBe("red");
        value.DisplayValue.ShouldBe("Red");
        value.SortOrder.ShouldBe(0);
        value.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        // Act
        var value = CreateTestOptionValue();

        // Assert
        value.ColorCode.ShouldBeNull();
        value.SwatchUrl.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldNormalizeValue()
    {
        // Act
        var value = CreateTestOptionValue(value: "Sky Blue");

        // Assert
        value.Value.ShouldBe("sky_blue");
    }

    [Fact]
    public void Create_ShouldLowercaseValue()
    {
        // Act
        var value = CreateTestOptionValue(value: "RED");

        // Assert
        value.Value.ShouldBe("red");
    }

    [Fact]
    public void Create_WithNullDisplayValue_ShouldUseValueAsDisplay()
    {
        // Arrange
        var option = CreateTestOption();

        // Act
        var value = option.AddValue("red");

        // Assert
        value.DisplayValue.ShouldBe("red");
    }

    [Fact]
    public void Create_SecondValue_ShouldIncrementSortOrder()
    {
        // Arrange
        var option = CreateTestOption();
        option.AddValue("red", "Red");

        // Act
        var second = option.AddValue("blue", "Blue");

        // Assert
        second.SortOrder.ShouldBe(1);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldUpdateAllFields()
    {
        // Arrange
        var value = CreateTestOptionValue(value: "red", displayValue: "Red");

        // Act
        value.Update("blue", "Blue", 5);

        // Assert
        value.Value.ShouldBe("blue");
        value.DisplayValue.ShouldBe("Blue");
        value.SortOrder.ShouldBe(5);
    }

    [Fact]
    public void Update_ShouldNormalizeValue()
    {
        // Arrange
        var value = CreateTestOptionValue();

        // Act
        value.Update("Dark Green", "Dark Green", 0);

        // Assert
        value.Value.ShouldBe("dark_green");
    }

    [Fact]
    public void Update_ShouldLowercaseValue()
    {
        // Arrange
        var value = CreateTestOptionValue();

        // Act
        value.Update("PURPLE", "Purple", 0);

        // Assert
        value.Value.ShouldBe("purple");
    }

    [Fact]
    public void Update_WithNullDisplayValue_ShouldUseValue()
    {
        // Arrange
        var value = CreateTestOptionValue();

        // Act
        value.Update("teal", null, 0);

        // Assert
        value.DisplayValue.ShouldBe("teal");
    }

    #endregion

    #region SetColorCode Tests

    [Fact]
    public void SetColorCode_ShouldSetHexCode()
    {
        // Arrange
        var value = CreateTestOptionValue();

        // Act
        value.SetColorCode("#FF0000");

        // Assert
        value.ColorCode.ShouldBe("#FF0000");
    }

    [Fact]
    public void SetColorCode_WithNull_ShouldClearColorCode()
    {
        // Arrange
        var value = CreateTestOptionValue();
        value.SetColorCode("#FF0000");

        // Act
        value.SetColorCode(null);

        // Assert
        value.ColorCode.ShouldBeNull();
    }

    #endregion

    #region SetSwatchUrl Tests

    [Fact]
    public void SetSwatchUrl_ShouldSetUrl()
    {
        // Arrange
        var value = CreateTestOptionValue();

        // Act
        value.SetSwatchUrl("https://example.com/swatch-red.jpg");

        // Assert
        value.SwatchUrl.ShouldBe("https://example.com/swatch-red.jpg");
    }

    [Fact]
    public void SetSwatchUrl_WithNull_ShouldClearSwatchUrl()
    {
        // Arrange
        var value = CreateTestOptionValue();
        value.SetSwatchUrl("https://example.com/swatch.jpg");

        // Act
        value.SetSwatchUrl(null);

        // Assert
        value.SwatchUrl.ShouldBeNull();
    }

    #endregion
}
