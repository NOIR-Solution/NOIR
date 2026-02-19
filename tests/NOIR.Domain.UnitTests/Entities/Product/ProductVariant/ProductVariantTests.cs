using NOIR.Domain.Entities.Product;

namespace NOIR.Domain.UnitTests.Entities.Product.ProductVariant;

/// <summary>
/// Unit tests for the ProductVariant entity.
/// Tests creation via parent Product, update methods, stock management,
/// pricing (compare-at, cost), options serialization, computed properties,
/// sort order, and image association.
/// ProductVariant.Create is internal, so instances are created via Product.AddVariant.
/// </summary>
public class ProductVariantTests
{
    private const string TestTenantId = "test-tenant";

    #region Helper Methods

    private static Domain.Entities.Product.Product CreateTestProduct()
    {
        return Domain.Entities.Product.Product.Create("Test Product", "test-product", 100_000m, "VND", TestTenantId);
    }

    private static Domain.Entities.Product.ProductVariant CreateTestVariant(
        string name = "Default",
        decimal price = 100_000m,
        string? sku = "SKU-001",
        Dictionary<string, string>? options = null)
    {
        var product = CreateTestProduct();
        return product.AddVariant(name, price, sku, options);
    }

    #endregion

    #region Creation Tests (via Product.AddVariant)

    [Fact]
    public void Create_ViaProduct_ShouldSetAllProperties()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        var variant = product.AddVariant("Size M", 120_000m, "SKU-M");

        // Assert
        variant.Should().NotBeNull();
        variant.Id.Should().NotBe(Guid.Empty);
        variant.ProductId.Should().Be(product.Id);
        variant.Name.Should().Be("Size M");
        variant.Price.Should().Be(120_000m);
        variant.Sku.Should().Be("SKU-M");
        variant.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        // Act
        var variant = CreateTestVariant();

        // Assert
        variant.StockQuantity.Should().Be(0);
        variant.CompareAtPrice.Should().BeNull();
        variant.CostPrice.Should().BeNull();
        variant.SortOrder.Should().Be(0);
        variant.ImageId.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullSku_ShouldAllowNull()
    {
        // Act
        var variant = CreateTestVariant(sku: null);

        // Assert
        variant.Sku.Should().BeNull();
    }

    [Fact]
    public void Create_WithOptions_ShouldSerializeOptions()
    {
        // Arrange
        var options = new Dictionary<string, string>
        {
            { "color", "Red" },
            { "size", "M" }
        };

        // Act
        var variant = CreateTestVariant(options: options);

        // Assert
        var parsed = variant.GetOptions();
        parsed.Should().HaveCount(2);
        parsed.Should().ContainKey("color").WhoseValue.Should().Be("Red");
        parsed.Should().ContainKey("size").WhoseValue.Should().Be("M");
    }

    [Fact]
    public void Create_WithoutOptions_ShouldHaveNullOptionsJson()
    {
        // Act
        var variant = CreateTestVariant(options: null);

        // Assert
        variant.GetOptions().Should().BeEmpty();
    }

    #endregion

    #region UpdateDetails Tests

    [Fact]
    public void UpdateDetails_ShouldUpdateNamePriceAndSku()
    {
        // Arrange
        var variant = CreateTestVariant(name: "Original", price: 100m, sku: "OLD-SKU");

        // Act
        variant.UpdateDetails("Updated", 200m, "NEW-SKU");

        // Assert
        variant.Name.Should().Be("Updated");
        variant.Price.Should().Be(200m);
        variant.Sku.Should().Be("NEW-SKU");
    }

    [Fact]
    public void UpdateDetails_WithNullSku_ShouldClearSku()
    {
        // Arrange
        var variant = CreateTestVariant(sku: "HAS-SKU");

        // Act
        variant.UpdateDetails("Name", 100m, null);

        // Assert
        variant.Sku.Should().BeNull();
    }

    #endregion

    #region SetCompareAtPrice Tests

    [Fact]
    public void SetCompareAtPrice_HigherThanPrice_ShouldEnableOnSale()
    {
        // Arrange
        var variant = CreateTestVariant(price: 100_000m);

        // Act
        variant.SetCompareAtPrice(150_000m);

        // Assert
        variant.CompareAtPrice.Should().Be(150_000m);
        variant.OnSale.Should().BeTrue();
    }

    [Fact]
    public void SetCompareAtPrice_LowerThanPrice_ShouldNotBeOnSale()
    {
        // Arrange
        var variant = CreateTestVariant(price: 100_000m);

        // Act
        variant.SetCompareAtPrice(50_000m);

        // Assert
        variant.CompareAtPrice.Should().Be(50_000m);
        variant.OnSale.Should().BeFalse();
    }

    [Fact]
    public void SetCompareAtPrice_EqualToPrice_ShouldNotBeOnSale()
    {
        // Arrange
        var variant = CreateTestVariant(price: 100_000m);

        // Act
        variant.SetCompareAtPrice(100_000m);

        // Assert
        variant.OnSale.Should().BeFalse();
    }

    [Fact]
    public void SetCompareAtPrice_WithNull_ShouldClearAndNotBeOnSale()
    {
        // Arrange
        var variant = CreateTestVariant(price: 100_000m);
        variant.SetCompareAtPrice(150_000m);

        // Act
        variant.SetCompareAtPrice(null);

        // Assert
        variant.CompareAtPrice.Should().BeNull();
        variant.OnSale.Should().BeFalse();
    }

    #endregion

    #region SetCostPrice Tests

    [Fact]
    public void SetCostPrice_ShouldSetCostPrice()
    {
        // Arrange
        var variant = CreateTestVariant();

        // Act
        variant.SetCostPrice(50_000m);

        // Assert
        variant.CostPrice.Should().Be(50_000m);
    }

    [Fact]
    public void SetCostPrice_WithNull_ShouldClearCostPrice()
    {
        // Arrange
        var variant = CreateTestVariant();
        variant.SetCostPrice(50_000m);

        // Act
        variant.SetCostPrice(null);

        // Assert
        variant.CostPrice.Should().BeNull();
    }

    #endregion

    #region Stock Management Tests

    [Fact]
    public void SetStock_WithValidQuantity_ShouldSetStock()
    {
        // Arrange
        var variant = CreateTestVariant();

        // Act
        variant.SetStock(10);

        // Assert
        variant.StockQuantity.Should().Be(10);
    }

    [Fact]
    public void SetStock_WithZero_ShouldSetToZero()
    {
        // Arrange
        var variant = CreateTestVariant();
        variant.SetStock(10);

        // Act
        variant.SetStock(0);

        // Assert
        variant.StockQuantity.Should().Be(0);
    }

    [Fact]
    public void SetStock_WithNegative_ShouldThrow()
    {
        // Arrange
        var variant = CreateTestVariant();

        // Act
        var act = () => variant.SetStock(-1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Stock cannot be negative*");
    }

    [Fact]
    public void ReserveStock_WithSufficientStock_ShouldDecrement()
    {
        // Arrange
        var variant = CreateTestVariant();
        variant.SetStock(10);

        // Act
        variant.ReserveStock(3);

        // Assert
        variant.StockQuantity.Should().Be(7);
    }

    [Fact]
    public void ReserveStock_ExactStock_ShouldDecementToZero()
    {
        // Arrange
        var variant = CreateTestVariant();
        variant.SetStock(5);

        // Act
        variant.ReserveStock(5);

        // Assert
        variant.StockQuantity.Should().Be(0);
    }

    [Fact]
    public void ReserveStock_InsufficientStock_ShouldThrow()
    {
        // Arrange
        var variant = CreateTestVariant();
        variant.SetStock(2);

        // Act
        var act = () => variant.ReserveStock(5);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insufficient stock*");
    }

    [Fact]
    public void ReleaseStock_ShouldIncrementQuantity()
    {
        // Arrange
        var variant = CreateTestVariant();
        variant.SetStock(5);

        // Act
        variant.ReleaseStock(3);

        // Assert
        variant.StockQuantity.Should().Be(8);
    }

    [Fact]
    public void AdjustStock_PositiveDelta_ShouldIncrement()
    {
        // Arrange
        var variant = CreateTestVariant();
        variant.SetStock(5);

        // Act
        variant.AdjustStock(10);

        // Assert
        variant.StockQuantity.Should().Be(15);
    }

    [Fact]
    public void AdjustStock_NegativeDelta_WithinBounds_ShouldDecrement()
    {
        // Arrange
        var variant = CreateTestVariant();
        variant.SetStock(10);

        // Act
        variant.AdjustStock(-3);

        // Assert
        variant.StockQuantity.Should().Be(7);
    }

    [Fact]
    public void AdjustStock_NegativeDelta_BelowZero_ShouldThrow()
    {
        // Arrange
        var variant = CreateTestVariant();
        variant.SetStock(3);

        // Act
        var act = () => variant.AdjustStock(-5);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Stock cannot be negative*");
    }

    [Fact]
    public void AdjustStock_NegativeDelta_ExactlyToZero_ShouldSucceed()
    {
        // Arrange
        var variant = CreateTestVariant();
        variant.SetStock(5);

        // Act
        variant.AdjustStock(-5);

        // Assert
        variant.StockQuantity.Should().Be(0);
    }

    #endregion

    #region Computed Properties Tests

    [Fact]
    public void InStock_WithStock_ShouldBeTrue()
    {
        // Arrange
        var variant = CreateTestVariant();
        variant.SetStock(1);

        // Assert
        variant.InStock.Should().BeTrue();
    }

    [Fact]
    public void InStock_WithZeroStock_ShouldBeFalse()
    {
        // Arrange
        var variant = CreateTestVariant();

        // Assert
        variant.InStock.Should().BeFalse();
    }

    [Fact]
    public void LowStock_AtThresholdBoundary_ShouldBeTrue()
    {
        // Arrange - LowStock is true when stock > 0 and stock <= 5
        var variant = CreateTestVariant();
        variant.SetStock(5);

        // Assert
        variant.LowStock.Should().BeTrue();
    }

    [Fact]
    public void LowStock_AboveThreshold_ShouldBeFalse()
    {
        // Arrange
        var variant = CreateTestVariant();
        variant.SetStock(6);

        // Assert
        variant.LowStock.Should().BeFalse();
    }

    [Fact]
    public void LowStock_AtZero_ShouldBeFalse()
    {
        // Arrange - Zero is out of stock, not low stock
        var variant = CreateTestVariant();

        // Assert
        variant.LowStock.Should().BeFalse();
    }

    [Fact]
    public void LowStock_AtOne_ShouldBeTrue()
    {
        // Arrange
        var variant = CreateTestVariant();
        variant.SetStock(1);

        // Assert
        variant.LowStock.Should().BeTrue();
    }

    [Fact]
    public void OnSale_WithNoCompareAtPrice_ShouldBeFalse()
    {
        // Arrange
        var variant = CreateTestVariant(price: 100_000m);

        // Assert
        variant.OnSale.Should().BeFalse();
    }

    [Fact]
    public void OnSale_WithHigherCompareAtPrice_ShouldBeTrue()
    {
        // Arrange
        var variant = CreateTestVariant(price: 100_000m);
        variant.SetCompareAtPrice(150_000m);

        // Assert
        variant.OnSale.Should().BeTrue();
    }

    [Fact]
    public void OnSale_WithLowerCompareAtPrice_ShouldBeFalse()
    {
        // Arrange
        var variant = CreateTestVariant(price: 100_000m);
        variant.SetCompareAtPrice(80_000m);

        // Assert
        variant.OnSale.Should().BeFalse();
    }

    #endregion

    #region Options Serialization Tests

    [Fact]
    public void GetOptions_WithNullJson_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var variant = CreateTestVariant(options: null);

        // Assert
        variant.GetOptions().Should().BeEmpty();
    }

    [Fact]
    public void UpdateOptions_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var variant = CreateTestVariant();
        var options = new Dictionary<string, string>
        {
            { "color", "Blue" },
            { "size", "XL" }
        };

        // Act
        variant.UpdateOptions(options);

        // Assert
        var parsed = variant.GetOptions();
        parsed.Should().HaveCount(2);
        parsed.Should().ContainKey("color").WhoseValue.Should().Be("Blue");
        parsed.Should().ContainKey("size").WhoseValue.Should().Be("XL");
    }

    [Fact]
    public void UpdateOptions_ShouldOverwritePreviousOptions()
    {
        // Arrange
        var variant = CreateTestVariant(options: new Dictionary<string, string> { { "old", "value" } });

        // Act
        variant.UpdateOptions(new Dictionary<string, string> { { "new", "options" } });

        // Assert
        var parsed = variant.GetOptions();
        parsed.Should().HaveCount(1);
        parsed.Should().ContainKey("new").WhoseValue.Should().Be("options");
        parsed.Should().NotContainKey("old");
    }

    [Fact]
    public void UpdateOptions_EmptyDictionary_ShouldSetEmptyJson()
    {
        // Arrange
        var variant = CreateTestVariant();

        // Act
        variant.UpdateOptions(new Dictionary<string, string>());

        // Assert
        variant.GetOptions().Should().BeEmpty();
    }

    #endregion

    #region SetSortOrder Tests

    [Fact]
    public void SetSortOrder_ShouldUpdateValue()
    {
        // Arrange
        var variant = CreateTestVariant();

        // Act
        variant.SetSortOrder(5);

        // Assert
        variant.SortOrder.Should().Be(5);
    }

    #endregion

    #region SetImage Tests

    [Fact]
    public void SetImage_ShouldSetImageId()
    {
        // Arrange
        var variant = CreateTestVariant();
        var imageId = Guid.NewGuid();

        // Act
        variant.SetImage(imageId);

        // Assert
        variant.ImageId.Should().Be(imageId);
    }

    [Fact]
    public void SetImage_WithNull_ShouldClearImageId()
    {
        // Arrange
        var variant = CreateTestVariant();
        variant.SetImage(Guid.NewGuid());

        // Act
        variant.SetImage(null);

        // Assert
        variant.ImageId.Should().BeNull();
    }

    #endregion
}
