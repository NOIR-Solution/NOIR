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
        value.Should().NotBeNull();
        value.Id.Should().NotBe(Guid.Empty);
        value.OptionId.Should().Be(option.Id);
        value.Value.Should().Be("red");
        value.DisplayValue.Should().Be("Red");
        value.SortOrder.Should().Be(0);
        value.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        // Act
        var value = CreateTestOptionValue();

        // Assert
        value.ColorCode.Should().BeNull();
        value.SwatchUrl.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldNormalizeValue()
    {
        // Act
        var value = CreateTestOptionValue(value: "Sky Blue");

        // Assert
        value.Value.Should().Be("sky_blue");
    }

    [Fact]
    public void Create_ShouldLowercaseValue()
    {
        // Act
        var value = CreateTestOptionValue(value: "RED");

        // Assert
        value.Value.Should().Be("red");
    }

    [Fact]
    public void Create_WithNullDisplayValue_ShouldUseValueAsDisplay()
    {
        // Arrange
        var option = CreateTestOption();

        // Act
        var value = option.AddValue("red");

        // Assert
        value.DisplayValue.Should().Be("red");
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
        second.SortOrder.Should().Be(1);
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
        value.Value.Should().Be("blue");
        value.DisplayValue.Should().Be("Blue");
        value.SortOrder.Should().Be(5);
    }

    [Fact]
    public void Update_ShouldNormalizeValue()
    {
        // Arrange
        var value = CreateTestOptionValue();

        // Act
        value.Update("Dark Green", "Dark Green", 0);

        // Assert
        value.Value.Should().Be("dark_green");
    }

    [Fact]
    public void Update_ShouldLowercaseValue()
    {
        // Arrange
        var value = CreateTestOptionValue();

        // Act
        value.Update("PURPLE", "Purple", 0);

        // Assert
        value.Value.Should().Be("purple");
    }

    [Fact]
    public void Update_WithNullDisplayValue_ShouldUseValue()
    {
        // Arrange
        var value = CreateTestOptionValue();

        // Act
        value.Update("teal", null, 0);

        // Assert
        value.DisplayValue.Should().Be("teal");
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
        value.ColorCode.Should().Be("#FF0000");
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
        value.ColorCode.Should().BeNull();
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
        value.SwatchUrl.Should().Be("https://example.com/swatch-red.jpg");
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
        value.SwatchUrl.Should().BeNull();
    }

    #endregion
}
