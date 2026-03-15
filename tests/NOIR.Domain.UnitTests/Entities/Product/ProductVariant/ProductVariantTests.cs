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
        variant.ShouldNotBeNull();
        variant.Id.ShouldNotBe(Guid.Empty);
        variant.ProductId.ShouldBe(product.Id);
        variant.Name.ShouldBe("Size M");
        variant.Price.ShouldBe(120_000m);
        variant.Sku.ShouldBe("SKU-M");
        variant.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        // Act
        var variant = CreateTestVariant();

        // Assert
        variant.StockQuantity.ShouldBe(0);
        variant.CompareAtPrice.ShouldBeNull();
        variant.CostPrice.ShouldBeNull();
        variant.SortOrder.ShouldBe(0);
        variant.ImageId.ShouldBeNull();
    }

    [Fact]
    public void Create_WithNullSku_ShouldAllowNull()
    {
        // Act
        var variant = CreateTestVariant(sku: null);

        // Assert
        variant.Sku.ShouldBeNull();
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
        parsed.Count().ShouldBe(2);
        parsed.ShouldContainKeyAndValue("color", "Red");
        parsed.ShouldContainKeyAndValue("size", "M");
    }

    [Fact]
    public void Create_WithoutOptions_ShouldHaveNullOptionsJson()
    {
        // Act
        var variant = CreateTestVariant(options: null);

        // Assert
        variant.GetOptions().ShouldBeEmpty();
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
        variant.Name.ShouldBe("Updated");
        variant.Price.ShouldBe(200m);
        variant.Sku.ShouldBe("NEW-SKU");
    }

    [Fact]
    public void UpdateDetails_WithNullSku_ShouldClearSku()
    {
        // Arrange
        var variant = CreateTestVariant(sku: "HAS-SKU");

        // Act
        variant.UpdateDetails("Name", 100m, null);

        // Assert
        variant.Sku.ShouldBeNull();
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
        variant.CompareAtPrice.ShouldBe(150_000m);
        variant.OnSale.ShouldBeTrue();
    }

    [Fact]
    public void SetCompareAtPrice_LowerThanPrice_ShouldNotBeOnSale()
    {
        // Arrange
        var variant = CreateTestVariant(price: 100_000m);

        // Act
        variant.SetCompareAtPrice(50_000m);

        // Assert
        variant.CompareAtPrice.ShouldBe(50_000m);
        variant.OnSale.ShouldBeFalse();
    }

    [Fact]
    public void SetCompareAtPrice_EqualToPrice_ShouldNotBeOnSale()
    {
        // Arrange
        var variant = CreateTestVariant(price: 100_000m);

        // Act
        variant.SetCompareAtPrice(100_000m);

        // Assert
        variant.OnSale.ShouldBeFalse();
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
        variant.CompareAtPrice.ShouldBeNull();
        variant.OnSale.ShouldBeFalse();
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
        variant.CostPrice.ShouldBe(50_000m);
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
        variant.CostPrice.ShouldBeNull();
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
        variant.StockQuantity.ShouldBe(10);
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
        variant.StockQuantity.ShouldBe(0);
    }

    [Fact]
    public void SetStock_WithNegative_ShouldThrow()
    {
        // Arrange
        var variant = CreateTestVariant();

        // Act
        var act = () => variant.SetStock(-1);

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Stock cannot be negative");
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
        variant.StockQuantity.ShouldBe(7);
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
        variant.StockQuantity.ShouldBe(0);
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
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Insufficient stock");
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
        variant.StockQuantity.ShouldBe(8);
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
        variant.StockQuantity.ShouldBe(15);
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
        variant.StockQuantity.ShouldBe(7);
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
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Stock cannot be negative");
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
        variant.StockQuantity.ShouldBe(0);
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
        variant.InStock.ShouldBeTrue();
    }

    [Fact]
    public void InStock_WithZeroStock_ShouldBeFalse()
    {
        // Arrange
        var variant = CreateTestVariant();

        // Assert
        variant.InStock.ShouldBeFalse();
    }

    [Fact]
    public void LowStock_AtThresholdBoundary_ShouldBeTrue()
    {
        // Arrange - LowStock is true when stock > 0 and stock <= 5
        var variant = CreateTestVariant();
        variant.SetStock(5);

        // Assert
        variant.LowStock.ShouldBeTrue();
    }

    [Fact]
    public void LowStock_AboveThreshold_ShouldBeFalse()
    {
        // Arrange
        var variant = CreateTestVariant();
        variant.SetStock(6);

        // Assert
        variant.LowStock.ShouldBeFalse();
    }

    [Fact]
    public void LowStock_AtZero_ShouldBeFalse()
    {
        // Arrange - Zero is out of stock, not low stock
        var variant = CreateTestVariant();

        // Assert
        variant.LowStock.ShouldBeFalse();
    }

    [Fact]
    public void LowStock_AtOne_ShouldBeTrue()
    {
        // Arrange
        var variant = CreateTestVariant();
        variant.SetStock(1);

        // Assert
        variant.LowStock.ShouldBeTrue();
    }

    [Fact]
    public void OnSale_WithNoCompareAtPrice_ShouldBeFalse()
    {
        // Arrange
        var variant = CreateTestVariant(price: 100_000m);

        // Assert
        variant.OnSale.ShouldBeFalse();
    }

    [Fact]
    public void OnSale_WithHigherCompareAtPrice_ShouldBeTrue()
    {
        // Arrange
        var variant = CreateTestVariant(price: 100_000m);
        variant.SetCompareAtPrice(150_000m);

        // Assert
        variant.OnSale.ShouldBeTrue();
    }

    [Fact]
    public void OnSale_WithLowerCompareAtPrice_ShouldBeFalse()
    {
        // Arrange
        var variant = CreateTestVariant(price: 100_000m);
        variant.SetCompareAtPrice(80_000m);

        // Assert
        variant.OnSale.ShouldBeFalse();
    }

    #endregion

    #region Options Serialization Tests

    [Fact]
    public void GetOptions_WithNullJson_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var variant = CreateTestVariant(options: null);

        // Assert
        variant.GetOptions().ShouldBeEmpty();
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
        parsed.Count().ShouldBe(2);
        parsed.ShouldContainKeyAndValue("color", "Blue");
        parsed.ShouldContainKeyAndValue("size", "XL");
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
        parsed.Count().ShouldBe(1);
        parsed.ShouldContainKeyAndValue("new", "options");
        parsed.ShouldNotContainKey("old");
    }

    [Fact]
    public void UpdateOptions_EmptyDictionary_ShouldSetEmptyJson()
    {
        // Arrange
        var variant = CreateTestVariant();

        // Act
        variant.UpdateOptions(new Dictionary<string, string>());

        // Assert
        variant.GetOptions().ShouldBeEmpty();
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
        variant.SortOrder.ShouldBe(5);
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
        variant.ImageId.ShouldBe(imageId);
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
        variant.ImageId.ShouldBeNull();
    }

    #endregion
}
