using NOIR.Domain.Entities.Product;

namespace NOIR.Domain.UnitTests.Entities.Product.ProductFilterIndex;

/// <summary>
/// Unit tests for the ProductFilterIndex denormalized entity.
/// Tests factory methods, update methods for stock/pricing/rating,
/// attributes JSON, search text, category path, stale marking,
/// and full product synchronization.
/// </summary>
public class ProductFilterIndexTests
{
    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestProductId = Guid.NewGuid();

    #region Helper Methods

    private static Domain.Entities.Product.ProductFilterIndex CreateTestIndex(
        Guid? productId = null,
        string productName = "Test Product",
        string productSlug = "test-product",
        ProductStatus status = ProductStatus.Active,
        decimal basePrice = 100_000m,
        string currency = "VND",
        string? tenantId = TestTenantId)
    {
        return Domain.Entities.Product.ProductFilterIndex.Create(
            productId ?? TestProductId,
            productName,
            productSlug,
            status,
            basePrice,
            currency,
            tenantId);
    }

    private static Domain.Entities.Product.Product CreateProductForSync(
        string name = "Sync Product",
        string slug = "sync-product",
        decimal basePrice = 200_000m)
    {
        return Domain.Entities.Product.Product.Create(name, slug, basePrice, "VND", TestTenantId);
    }

    #endregion

    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidIndex()
    {
        // Act
        var index = CreateTestIndex();

        // Assert
        index.Should().NotBeNull();
        index.Id.Should().NotBe(Guid.Empty);
        index.ProductId.Should().Be(TestProductId);
        index.ProductName.Should().Be("Test Product");
        index.ProductSlug.Should().Be("test-product");
        index.Status.Should().Be(ProductStatus.Active);
        index.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetPricingFromBasePrice()
    {
        // Act
        var index = CreateTestIndex(basePrice: 150_000m);

        // Assert
        index.MinPrice.Should().Be(150_000m);
        index.MaxPrice.Should().Be(150_000m);
        index.Currency.Should().Be("VND");
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        // Act
        var index = CreateTestIndex();

        // Assert
        index.InStock.Should().BeFalse();
        index.TotalStock.Should().Be(0);
        index.AverageRating.Should().BeNull();
        index.ReviewCount.Should().Be(0);
        index.AttributesJson.Should().Be("{}");
        index.SearchText.Should().BeEmpty();
        index.PrimaryImageUrl.Should().BeNull();
        index.SortOrder.Should().Be(0);
        index.CategoryId.Should().BeNull();
        index.CategoryPath.Should().BeNull();
        index.BrandId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSetTimestamps()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var index = CreateTestIndex();

        // Assert
        index.LastSyncedAt.Should().BeOnOrAfter(before);
        index.ProductUpdatedAt.Should().BeOnOrAfter(before);
    }

    [Theory]
    [InlineData(ProductStatus.Draft)]
    [InlineData(ProductStatus.Active)]
    [InlineData(ProductStatus.Archived)]
    [InlineData(ProductStatus.OutOfStock)]
    public void Create_WithVariousStatuses_ShouldSetStatus(ProductStatus status)
    {
        // Act
        var index = CreateTestIndex(status: status);

        // Assert
        index.Status.Should().Be(status);
    }

    #endregion

    #region UpdateStock Tests

    [Fact]
    public void UpdateStock_ShouldSetStockAndInStockFlag()
    {
        // Arrange
        var index = CreateTestIndex();

        // Act
        index.UpdateStock(50, true);

        // Assert
        index.TotalStock.Should().Be(50);
        index.InStock.Should().BeTrue();
    }

    [Fact]
    public void UpdateStock_WithZeroStock_ShouldSetOutOfStock()
    {
        // Arrange
        var index = CreateTestIndex();
        index.UpdateStock(10, true);

        // Act
        index.UpdateStock(0, false);

        // Assert
        index.TotalStock.Should().Be(0);
        index.InStock.Should().BeFalse();
    }

    [Fact]
    public void UpdateStock_ShouldUpdateLastSyncedAt()
    {
        // Arrange
        var index = CreateTestIndex();
        var before = DateTime.UtcNow;

        // Act
        index.UpdateStock(10, true);

        // Assert
        index.LastSyncedAt.Should().BeOnOrAfter(before);
    }

    #endregion

    #region UpdatePricing Tests

    [Fact]
    public void UpdatePricing_ShouldSetMinMaxAndCurrency()
    {
        // Arrange
        var index = CreateTestIndex();

        // Act
        index.UpdatePricing(50_000m, 200_000m, "USD");

        // Assert
        index.MinPrice.Should().Be(50_000m);
        index.MaxPrice.Should().Be(200_000m);
        index.Currency.Should().Be("USD");
    }

    [Fact]
    public void UpdatePricing_ShouldUpdateLastSyncedAt()
    {
        // Arrange
        var index = CreateTestIndex();
        var before = DateTime.UtcNow;

        // Act
        index.UpdatePricing(100m, 200m, "VND");

        // Assert
        index.LastSyncedAt.Should().BeOnOrAfter(before);
    }

    #endregion

    #region UpdateRating Tests

    [Fact]
    public void UpdateRating_ShouldSetRatingAndReviewCount()
    {
        // Arrange
        var index = CreateTestIndex();

        // Act
        index.UpdateRating(4.5m, 120);

        // Assert
        index.AverageRating.Should().Be(4.5m);
        index.ReviewCount.Should().Be(120);
    }

    [Fact]
    public void UpdateRating_WithNullRating_ShouldSetNull()
    {
        // Arrange
        var index = CreateTestIndex();
        index.UpdateRating(4.0m, 10);

        // Act
        index.UpdateRating(null, 0);

        // Assert
        index.AverageRating.Should().BeNull();
        index.ReviewCount.Should().Be(0);
    }

    #endregion

    #region SetAttributesJson Tests

    [Fact]
    public void SetAttributesJson_ShouldSetJson()
    {
        // Arrange
        var index = CreateTestIndex();
        var json = """{"color": ["red", "blue"], "size": ["m", "l"]}""";

        // Act
        index.SetAttributesJson(json);

        // Assert
        index.AttributesJson.Should().Be(json);
    }

    [Fact]
    public void SetAttributesJson_WithNull_ShouldSetEmptyObject()
    {
        // Arrange
        var index = CreateTestIndex();

        // Act
        index.SetAttributesJson(null!);

        // Assert
        index.AttributesJson.Should().Be("{}");
    }

    [Fact]
    public void SetAttributesJson_ShouldUpdateLastSyncedAt()
    {
        // Arrange
        var index = CreateTestIndex();
        var before = DateTime.UtcNow;

        // Act
        index.SetAttributesJson("{}");

        // Assert
        index.LastSyncedAt.Should().BeOnOrAfter(before);
    }

    #endregion

    #region SetSearchText Tests

    [Fact]
    public void SetSearchText_ShouldSetText()
    {
        // Arrange
        var index = CreateTestIndex();

        // Act
        index.SetSearchText("iPhone 15 Pro Max Apple");

        // Assert
        index.SearchText.Should().Be("iPhone 15 Pro Max Apple");
    }

    [Fact]
    public void SetSearchText_WithNull_ShouldSetEmpty()
    {
        // Arrange
        var index = CreateTestIndex();

        // Act
        index.SetSearchText(null!);

        // Assert
        index.SearchText.Should().BeEmpty();
    }

    #endregion

    #region SetCategoryPath Tests

    [Fact]
    public void SetCategoryPath_ShouldSetPath()
    {
        // Arrange
        var index = CreateTestIndex();

        // Act
        index.SetCategoryPath("1/5/23");

        // Assert
        index.CategoryPath.Should().Be("1/5/23");
    }

    [Fact]
    public void SetCategoryPath_WithNull_ShouldClearPath()
    {
        // Arrange
        var index = CreateTestIndex();
        index.SetCategoryPath("1/5/23");

        // Act
        index.SetCategoryPath(null);

        // Assert
        index.CategoryPath.Should().BeNull();
    }

    #endregion

    #region MarkAsStale Tests

    [Fact]
    public void MarkAsStale_ShouldSetLastSyncedAtToMinValue()
    {
        // Arrange
        var index = CreateTestIndex();
        index.LastSyncedAt.Should().NotBe(DateTime.MinValue);

        // Act
        index.MarkAsStale();

        // Assert
        index.LastSyncedAt.Should().Be(DateTime.MinValue);
    }

    #endregion

    #region UpdateFromProduct Tests

    [Fact]
    public void UpdateFromProduct_ShouldSyncBasicInfo()
    {
        // Arrange
        var index = CreateTestIndex();
        var product = CreateProductForSync(name: "Updated Product", slug: "updated-product");

        // Act
        index.UpdateFromProduct(product, null, null);

        // Assert
        index.ProductName.Should().Be("Updated Product");
        index.ProductSlug.Should().Be("updated-product");
        index.Status.Should().Be(ProductStatus.Draft);
    }

    [Fact]
    public void UpdateFromProduct_WithCategory_ShouldSetCategoryInfo()
    {
        // Arrange
        var index = CreateTestIndex();
        var product = CreateProductForSync();
        var category = Domain.Entities.Product.ProductCategory.Create("Electronics", "electronics", null, TestTenantId);
        product.SetCategory(category.Id);

        // Act
        index.UpdateFromProduct(product, category, null, "1/5");

        // Assert
        index.CategoryId.Should().Be(category.Id);
        index.CategoryName.Should().Be("Electronics");
        index.CategorySlug.Should().Be("electronics");
        index.CategoryPath.Should().Be("1/5");
    }

    [Fact]
    public void UpdateFromProduct_WithBrand_ShouldSetBrandInfo()
    {
        // Arrange
        var index = CreateTestIndex();
        var product = CreateProductForSync();
        var brand = Domain.Entities.Product.Brand.Create("Nike", "nike", TestTenantId);
        product.SetBrandId(brand.Id);

        // Act
        index.UpdateFromProduct(product, null, brand);

        // Assert
        index.BrandId.Should().Be(brand.Id);
        index.BrandName.Should().Be("Nike");
        index.BrandSlug.Should().Be("nike");
    }

    [Fact]
    public void UpdateFromProduct_WithNoVariants_ShouldUseBaePrice()
    {
        // Arrange
        var index = CreateTestIndex();
        var product = CreateProductForSync(basePrice: 300_000m);

        // Act
        index.UpdateFromProduct(product, null, null);

        // Assert
        index.MinPrice.Should().Be(300_000m);
        index.MaxPrice.Should().Be(300_000m);
    }

    [Fact]
    public void UpdateFromProduct_WithVariants_ShouldUseVariantPricing()
    {
        // Arrange
        var index = CreateTestIndex();
        var product = CreateProductForSync(basePrice: 100_000m);
        product.AddVariant("Small", 80_000m);
        product.AddVariant("Large", 150_000m);

        // Act
        index.UpdateFromProduct(product, null, null);

        // Assert
        index.MinPrice.Should().Be(80_000m);
        index.MaxPrice.Should().Be(150_000m);
    }

    [Fact]
    public void UpdateFromProduct_ShouldSyncInventory()
    {
        // Arrange
        var index = CreateTestIndex();
        var product = CreateProductForSync();
        var v1 = product.AddVariant("V1", 100_000m);
        var v2 = product.AddVariant("V2", 200_000m);
        v1.SetStock(10);
        v2.SetStock(5);

        // Act
        index.UpdateFromProduct(product, null, null);

        // Assert
        index.TotalStock.Should().Be(15);
        index.InStock.Should().BeTrue();
    }

    [Fact]
    public void UpdateFromProduct_ShouldUpdateSearchText()
    {
        // Arrange
        var index = CreateTestIndex();
        var product = CreateProductForSync(name: "iPhone 15", slug: "iphone-15");

        // Act
        index.UpdateFromProduct(product, null, null);

        // Assert
        index.SearchText.Should().Contain("iPhone 15");
        index.SearchText.Should().Contain("iphone-15");
    }

    [Fact]
    public void UpdateFromProduct_ShouldUpdateTimestamps()
    {
        // Arrange
        var index = CreateTestIndex();
        var product = CreateProductForSync();
        var before = DateTime.UtcNow;

        // Act
        index.UpdateFromProduct(product, null, null);

        // Assert
        index.LastSyncedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void UpdateFromProduct_WithLegacyBrandString_ShouldFallbackToBrandProperty()
    {
        // Arrange
        var index = CreateTestIndex();
        var product = CreateProductForSync();
        product.SetBrand("Legacy Brand Name");

        // Act
        index.UpdateFromProduct(product, null, null);

        // Assert
        index.BrandName.Should().Be("Legacy Brand Name");
    }

    #endregion
}
