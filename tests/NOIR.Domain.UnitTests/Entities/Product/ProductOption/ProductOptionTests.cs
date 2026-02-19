using NOIR.Domain.Entities.Product;

namespace NOIR.Domain.UnitTests.Entities.Product.ProductOption;

/// <summary>
/// Unit tests for the ProductOption entity.
/// Tests creation via parent Product, update methods, name normalization,
/// value management (add/remove), and sort order behavior.
/// ProductOption.Create is internal, so instances are created via Product.AddOption.
/// </summary>
public class ProductOptionTests
{
    private const string TestTenantId = "test-tenant";

    #region Helper Methods

    private static Domain.Entities.Product.Product CreateTestProduct()
    {
        return Domain.Entities.Product.Product.Create("Test Product", "test-product", 100_000m, "VND", TestTenantId);
    }

    private static Domain.Entities.Product.ProductOption CreateTestOption(
        string name = "Color",
        string? displayName = null)
    {
        var product = CreateTestProduct();
        return product.AddOption(name, displayName);
    }

    #endregion

    #region Creation Tests (via Product.AddOption)

    [Fact]
    public void Create_ViaProduct_ShouldSetAllProperties()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        var option = product.AddOption("Color", "Color");

        // Assert
        option.Should().NotBeNull();
        option.Id.Should().NotBe(Guid.Empty);
        option.ProductId.Should().Be(product.Id);
        option.Name.Should().Be("color");
        option.DisplayName.Should().Be("Color");
        option.SortOrder.Should().Be(0);
        option.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldNormalizeName()
    {
        // Act
        var option = CreateTestOption(name: "Shoe Size");

        // Assert
        option.Name.Should().Be("shoe_size");
    }

    [Fact]
    public void Create_ShouldLowercaseName()
    {
        // Act
        var option = CreateTestOption(name: "COLOR");

        // Assert
        option.Name.Should().Be("color");
    }

    [Fact]
    public void Create_WithNullDisplayName_ShouldUseNameAsDisplayName()
    {
        // Act
        var option = CreateTestOption(name: "Material", displayName: null);

        // Assert
        option.DisplayName.Should().Be("Material");
    }

    [Fact]
    public void Create_WithDisplayName_ShouldUseProvidedDisplayName()
    {
        // Act
        var option = CreateTestOption(name: "size", displayName: "Clothing Size");

        // Assert
        option.DisplayName.Should().Be("Clothing Size");
    }

    [Fact]
    public void Create_FirstOption_ShouldHaveSortOrderZero()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        var option = product.AddOption("Color");

        // Assert
        option.SortOrder.Should().Be(0);
    }

    [Fact]
    public void Create_SecondOption_ShouldIncrementSortOrder()
    {
        // Arrange
        var product = CreateTestProduct();
        product.AddOption("Color");

        // Act
        var second = product.AddOption("Size");

        // Assert
        second.SortOrder.Should().Be(1);
    }

    [Fact]
    public void Create_ThirdOption_ShouldContinueIncrementingSortOrder()
    {
        // Arrange
        var product = CreateTestProduct();
        product.AddOption("Color");
        product.AddOption("Size");

        // Act
        var third = product.AddOption("Material");

        // Assert
        third.SortOrder.Should().Be(2);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldUpdateNameDisplayNameAndSortOrder()
    {
        // Arrange
        var option = CreateTestOption(name: "Color");

        // Act
        option.Update("Shoe Size", "Shoe Size", 3);

        // Assert
        option.Name.Should().Be("shoe_size");
        option.DisplayName.Should().Be("Shoe Size");
        option.SortOrder.Should().Be(3);
    }

    [Fact]
    public void Update_ShouldNormalizeName()
    {
        // Arrange
        var option = CreateTestOption();

        // Act
        option.Update("Screen Resolution", null, 0);

        // Assert
        option.Name.Should().Be("screen_resolution");
    }

    [Fact]
    public void Update_WithNullDisplayName_ShouldUseName()
    {
        // Arrange
        var option = CreateTestOption();

        // Act
        option.Update("Material", null, 0);

        // Assert
        option.DisplayName.Should().Be("Material");
    }

    [Fact]
    public void Update_WithDisplayName_ShouldUseProvidedDisplayName()
    {
        // Arrange
        var option = CreateTestOption();

        // Act
        option.Update("size", "Clothing Size", 1);

        // Assert
        option.DisplayName.Should().Be("Clothing Size");
    }

    #endregion

    #region AddValue Tests

    [Fact]
    public void AddValue_ShouldAddValueToCollection()
    {
        // Arrange
        var option = CreateTestOption(name: "Color");

        // Act
        var value = option.AddValue("red", "Red");

        // Assert
        option.Values.Should().ContainSingle();
        value.Should().NotBeNull();
        value.OptionId.Should().Be(option.Id);
        value.Value.Should().Be("red");
        value.DisplayValue.Should().Be("Red");
    }

    [Fact]
    public void AddValue_FirstValue_ShouldHaveSortOrderZero()
    {
        // Arrange
        var option = CreateTestOption();

        // Act
        var value = option.AddValue("red", "Red");

        // Assert
        value.SortOrder.Should().Be(0);
    }

    [Fact]
    public void AddValue_SecondValue_ShouldIncrementSortOrder()
    {
        // Arrange
        var option = CreateTestOption();
        option.AddValue("red", "Red");

        // Act
        var second = option.AddValue("blue", "Blue");

        // Assert
        second.SortOrder.Should().Be(1);
    }

    [Fact]
    public void AddValue_MultipleValues_ShouldAddAll()
    {
        // Arrange
        var option = CreateTestOption();

        // Act
        option.AddValue("red", "Red");
        option.AddValue("blue", "Blue");
        option.AddValue("green", "Green");

        // Assert
        option.Values.Should().HaveCount(3);
    }

    [Fact]
    public void AddValue_WithNullDisplayValue_ShouldUseValueAsDisplay()
    {
        // Arrange
        var option = CreateTestOption();

        // Act
        var value = option.AddValue("red");

        // Assert
        value.DisplayValue.Should().Be("red");
    }

    [Fact]
    public void AddValue_ShouldSetTenantId()
    {
        // Arrange
        var option = CreateTestOption();

        // Act
        var value = option.AddValue("red", "Red");

        // Assert
        value.TenantId.Should().Be(TestTenantId);
    }

    #endregion

    #region RemoveValue Tests

    [Fact]
    public void RemoveValue_ExistingValue_ShouldRemove()
    {
        // Arrange
        var option = CreateTestOption();
        var value = option.AddValue("red", "Red");

        // Act
        option.RemoveValue(value.Id);

        // Assert
        option.Values.Should().BeEmpty();
    }

    [Fact]
    public void RemoveValue_NonExistingId_ShouldDoNothing()
    {
        // Arrange
        var option = CreateTestOption();
        option.AddValue("red", "Red");

        // Act
        option.RemoveValue(Guid.NewGuid());

        // Assert
        option.Values.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveValue_OneOfMultiple_ShouldKeepOthers()
    {
        // Arrange
        var option = CreateTestOption();
        option.AddValue("red", "Red");
        var blue = option.AddValue("blue", "Blue");
        option.AddValue("green", "Green");

        // Act
        option.RemoveValue(blue.Id);

        // Assert
        option.Values.Should().HaveCount(2);
        option.Values.Should().NotContain(v => v.Value == "blue");
    }

    #endregion
}
