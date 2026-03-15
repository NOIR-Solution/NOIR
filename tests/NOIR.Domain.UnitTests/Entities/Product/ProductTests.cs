using NOIR.Domain.Entities.Product;
using NOIR.Domain.Events.Product;

namespace NOIR.Domain.UnitTests.Entities.Product;

/// <summary>
/// Unit tests for the Product entity.
/// Tests factory methods, status transitions, variants, images, options, and domain events.
/// </summary>
public class ProductTests
{
    private const string TestTenantId = "test-tenant";

    private static Domain.Entities.Product.Product CreateTestProduct(
        string name = "Test Product",
        string slug = "test-product",
        decimal basePrice = 100_000m,
        string currency = "VND",
        string? tenantId = TestTenantId)
    {
        return Domain.Entities.Product.Product.Create(name, slug, basePrice, currency, tenantId);
    }

    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidProduct()
    {
        // Act
        var product = CreateTestProduct();

        // Assert
        product.ShouldNotBeNull();
        product.Id.ShouldNotBe(Guid.Empty);
        product.Name.ShouldBe("Test Product");
        product.Slug.ShouldBe("test-product");
        product.BasePrice.ShouldBe(100_000m);
        product.Currency.ShouldBe("VND");
        product.Status.ShouldBe(ProductStatus.Draft);
        product.TrackInventory.ShouldBeTrue();
        product.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldLowercaseSlug()
    {
        // Act
        var product = Domain.Entities.Product.Product.Create("Test", "My-PRODUCT-Slug", 100m);

        // Assert
        product.Slug.ShouldBe("my-product-slug");
    }

    [Fact]
    public void Create_ShouldRaiseDomainEvent()
    {
        // Act
        var product = CreateTestProduct();

        // Assert
        product.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ProductCreatedEvent>()
            .ProductId.ShouldBe(product.Id);
    }

    [Fact]
    public void Create_WithDefaultCurrency_ShouldUseVND()
    {
        // Act
        var product = Domain.Entities.Product.Product.Create("Test", "test", 100m);

        // Assert
        product.Currency.ShouldBe("VND");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrow(string? name)
    {
        // Act & Assert
        var act = () => Domain.Entities.Product.Product.Create(name!, "slug", 100m);
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidSlug_ShouldThrow(string? slug)
    {
        // Act & Assert
        var act = () => Domain.Entities.Product.Product.Create("Name", slug!, 100m);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_WithNegativePrice_ShouldThrow()
    {
        // Act & Assert
        var act = () => Domain.Entities.Product.Product.Create("Name", "slug", -1m);
        Should.Throw<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Create_WithZeroPrice_ShouldSucceed()
    {
        // Act
        var product = Domain.Entities.Product.Product.Create("Free Product", "free", 0m);

        // Assert
        product.BasePrice.ShouldBe(0m);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void UpdateBasicInfo_ShouldUpdateFields()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.UpdateBasicInfo("New Name", "new-slug", "Short desc", "Full desc", "<p>HTML</p>");

        // Assert
        product.Name.ShouldBe("New Name");
        product.Slug.ShouldBe("new-slug");
        product.ShortDescription.ShouldBe("Short desc");
        product.Description.ShouldBe("Full desc");
        product.DescriptionHtml.ShouldBe("<p>HTML</p>");
    }

    [Fact]
    public void UpdateBasicInfo_ShouldLowercaseSlug()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.UpdateBasicInfo("Name", "NEW-Slug", null, null, null);

        // Assert
        product.Slug.ShouldBe("new-slug");
    }

    [Fact]
    public void UpdateBasicInfo_ShouldRaiseDomainEvent()
    {
        // Arrange
        var product = CreateTestProduct();
        product.ClearDomainEvents();

        // Act
        product.UpdateBasicInfo("Updated", "updated", null, null, null);

        // Assert
        product.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ProductUpdatedEvent>();
    }

    [Fact]
    public void UpdateBasicInfo_ShouldTrimShortDescription()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.UpdateBasicInfo("Name", "slug", "  trimmed  ", null, null);

        // Assert
        product.ShortDescription.ShouldBe("trimmed");
    }

    [Fact]
    public void UpdatePricing_ShouldUpdatePriceAndCurrency()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.UpdatePricing(200_000m, "USD");

        // Assert
        product.BasePrice.ShouldBe(200_000m);
        product.Currency.ShouldBe("USD");
    }

    [Fact]
    public void UpdatePricing_ShouldRaiseDomainEvent()
    {
        // Arrange
        var product = CreateTestProduct();
        product.ClearDomainEvents();

        // Act
        product.UpdatePricing(500m);

        // Assert
        product.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ProductUpdatedEvent>();
    }

    [Fact]
    public void SetCategory_ShouldSetCategoryId()
    {
        // Arrange
        var product = CreateTestProduct();
        var categoryId = Guid.NewGuid();

        // Act
        product.SetCategory(categoryId);

        // Assert
        product.CategoryId.ShouldBe(categoryId);
    }

    [Fact]
    public void SetCategory_WithNull_ShouldClearCategory()
    {
        // Arrange
        var product = CreateTestProduct();
        product.SetCategory(Guid.NewGuid());

        // Act
        product.SetCategory(null);

        // Assert
        product.CategoryId.ShouldBeNull();
    }

    [Fact]
    public void SetBrand_ShouldSetBrandString()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.SetBrand("Nike");

        // Assert
        product.Brand.ShouldBe("Nike");
    }

    [Fact]
    public void SetBrandId_ShouldSetBrandIdAndRaiseEvent()
    {
        // Arrange
        var product = CreateTestProduct();
        product.ClearDomainEvents();
        var brandId = Guid.NewGuid();

        // Act
        product.SetBrandId(brandId);

        // Assert
        product.BrandId.ShouldBe(brandId);
        product.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ProductUpdatedEvent>();
    }

    [Fact]
    public void UpdateIdentification_ShouldSetSkuAndBarcode()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.UpdateIdentification("SKU-001", "BARCODE-001");

        // Assert
        product.Sku.ShouldBe("SKU-001");
        product.Barcode.ShouldBe("BARCODE-001");
    }

    [Fact]
    public void UpdateIdentification_WithWhitespace_ShouldTrimOrNullify()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.UpdateIdentification("  SKU  ", "   ");

        // Assert
        product.Sku.ShouldBe("SKU");
        product.Barcode.ShouldBeNull();
    }

    [Fact]
    public void UpdateSeo_ShouldSetMetaFields()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.UpdateSeo("SEO Title", "SEO Description");

        // Assert
        product.MetaTitle.ShouldBe("SEO Title");
        product.MetaDescription.ShouldBe("SEO Description");
    }

    [Fact]
    public void UpdatePhysicalProperties_ShouldSetAllFields()
    {
        // Arrange
        var product = CreateTestProduct();
        product.ClearDomainEvents();

        // Act
        product.UpdatePhysicalProperties(1.5m, "kg", 30m, 20m, 10m, "cm");

        // Assert
        product.Weight.ShouldBe(1.5m);
        product.WeightUnit.ShouldBe("kg");
        product.Length.ShouldBe(30m);
        product.Width.ShouldBe(20m);
        product.Height.ShouldBe(10m);
        product.DimensionUnit.ShouldBe("cm");
        product.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ProductUpdatedEvent>();
    }

    [Fact]
    public void SetInventoryTracking_ShouldUpdateFlag()
    {
        // Arrange
        var product = CreateTestProduct();
        product.TrackInventory.ShouldBeTrue();

        // Act
        product.SetInventoryTracking(false);

        // Assert
        product.TrackInventory.ShouldBeFalse();
    }

    #endregion

    #region Status Transition Tests

    [Fact]
    public void Publish_WhenDraft_ShouldSetActive()
    {
        // Arrange
        var product = CreateTestProduct();
        product.Status.ShouldBe(ProductStatus.Draft);
        product.ClearDomainEvents();

        // Act
        product.Publish();

        // Assert
        product.Status.ShouldBe(ProductStatus.Active);
        product.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ProductPublishedEvent>();
    }

    [Fact]
    public void Publish_WhenAlreadyActive_ShouldNotChange()
    {
        // Arrange
        var product = CreateTestProduct();
        product.Publish();
        product.ClearDomainEvents();

        // Act
        product.Publish();

        // Assert
        product.Status.ShouldBe(ProductStatus.Active);
        product.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Archive_ShouldSetArchived()
    {
        // Arrange
        var product = CreateTestProduct();
        product.ClearDomainEvents();

        // Act
        product.Archive();

        // Assert
        product.Status.ShouldBe(ProductStatus.Archived);
        product.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ProductArchivedEvent>();
    }

    [Fact]
    public void SetOutOfStock_WhenNoVariants_ShouldSetStatus()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.SetOutOfStock();

        // Assert — TotalStock is 0 (no variants), so it should set OutOfStock
        product.Status.ShouldBe(ProductStatus.OutOfStock);
    }

    [Fact]
    public void SetOutOfStock_WhenVariantsHaveStock_ShouldNotChange()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("Default", 100m, "SKU-1");
        variant.SetStock(10);
        product.Publish();

        // Act
        product.SetOutOfStock();

        // Assert — TotalStock > 0, should not change
        product.Status.ShouldBe(ProductStatus.Active);
    }

    [Fact]
    public void RestoreFromOutOfStock_WhenHasStock_ShouldSetActive()
    {
        // Arrange
        var product = CreateTestProduct();
        product.SetOutOfStock();
        product.Status.ShouldBe(ProductStatus.OutOfStock);

        var variant = product.AddVariant("Default", 100m, "SKU-1");
        variant.SetStock(5);

        // Act
        product.RestoreFromOutOfStock();

        // Assert
        product.Status.ShouldBe(ProductStatus.Active);
    }

    [Fact]
    public void RestoreFromOutOfStock_WhenNotOutOfStock_ShouldNotChange()
    {
        // Arrange
        var product = CreateTestProduct();
        product.Status.ShouldBe(ProductStatus.Draft);

        // Act
        product.RestoreFromOutOfStock();

        // Assert
        product.Status.ShouldBe(ProductStatus.Draft);
    }

    #endregion

    #region Variant Tests

    [Fact]
    public void AddVariant_ShouldAddToCollection()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        var variant = product.AddVariant("Size M", 120_000m, "SKU-M");

        // Assert
        product.Variants.ShouldHaveSingleItem();
        variant.Name.ShouldBe("Size M");
        variant.Price.ShouldBe(120_000m);
        variant.Sku.ShouldBe("SKU-M");
        variant.ProductId.ShouldBe(product.Id);
        variant.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void AddVariant_WithOptions_ShouldSerializeOptions()
    {
        // Arrange
        var product = CreateTestProduct();
        var options = new Dictionary<string, string> { { "color", "Red" }, { "size", "M" } };

        // Act
        var variant = product.AddVariant("Red M", 120_000m, options: options);

        // Assert
        var parsed = variant.GetOptions();
        parsed.ShouldContainKeyAndValue("color", "Red");
        parsed.ShouldContainKeyAndValue("size", "M");
    }

    [Fact]
    public void RemoveVariant_WithExistingId_ShouldRemove()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);

        // Act
        product.RemoveVariant(variant.Id);

        // Assert
        product.Variants.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveVariant_WithNonExistingId_ShouldDoNothing()
    {
        // Arrange
        var product = CreateTestProduct();
        product.AddVariant("V1", 100m);

        // Act
        product.RemoveVariant(Guid.NewGuid());

        // Assert
        product.Variants.Count().ShouldBe(1);
    }

    [Fact]
    public void HasVariants_ShouldReflectCollection()
    {
        // Arrange
        var product = CreateTestProduct();
        product.HasVariants.ShouldBeFalse();

        // Act
        product.AddVariant("V1", 100m);

        // Assert
        product.HasVariants.ShouldBeTrue();
    }

    [Fact]
    public void TotalStock_ShouldSumAllVariants()
    {
        // Arrange
        var product = CreateTestProduct();
        var v1 = product.AddVariant("V1", 100m);
        var v2 = product.AddVariant("V2", 200m);
        v1.SetStock(10);
        v2.SetStock(5);

        // Assert
        product.TotalStock.ShouldBe(15);
        product.InStock.ShouldBeTrue();
    }

    #endregion

    #region Image Tests

    [Fact]
    public void AddImage_ShouldAddToCollection()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        var image = product.AddImage("https://example.com/img.jpg", "Alt text");

        // Assert
        product.Images.ShouldHaveSingleItem();
        image.Url.ShouldBe("https://example.com/img.jpg");
        image.AltText.ShouldBe("Alt text");
        image.SortOrder.ShouldBe(0);
        image.IsPrimary.ShouldBeFalse();
    }

    [Fact]
    public void AddImage_AsPrimary_ShouldClearOtherPrimaries()
    {
        // Arrange
        var product = CreateTestProduct();
        var first = product.AddImage("https://example.com/1.jpg", isPrimary: true);

        // Act
        var second = product.AddImage("https://example.com/2.jpg", isPrimary: true);

        // Assert
        first.IsPrimary.ShouldBeFalse();
        second.IsPrimary.ShouldBeTrue();
    }

    [Fact]
    public void AddImage_ShouldIncrementSortOrder()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        var first = product.AddImage("https://example.com/1.jpg");
        var second = product.AddImage("https://example.com/2.jpg");

        // Assert
        first.SortOrder.ShouldBe(0);
        second.SortOrder.ShouldBe(1);
    }

    [Fact]
    public void RemoveImage_ShouldRemoveFromCollection()
    {
        // Arrange
        var product = CreateTestProduct();
        var image = product.AddImage("https://example.com/img.jpg");

        // Act
        product.RemoveImage(image.Id);

        // Assert
        product.Images.ShouldBeEmpty();
    }

    [Fact]
    public void SetPrimaryImage_ShouldClearOthersAndSetTarget()
    {
        // Arrange
        var product = CreateTestProduct();
        var img1 = product.AddImage("https://example.com/1.jpg", isPrimary: true);
        var img2 = product.AddImage("https://example.com/2.jpg");

        // Act
        product.SetPrimaryImage(img2.Id);

        // Assert
        img1.IsPrimary.ShouldBeFalse();
        img2.IsPrimary.ShouldBeTrue();
    }

    [Fact]
    public void PrimaryImage_ShouldReturnFirstPrimary()
    {
        // Arrange
        var product = CreateTestProduct();
        product.AddImage("https://example.com/1.jpg");
        product.AddImage("https://example.com/2.jpg", isPrimary: true);

        // Assert
        product.PrimaryImage!.IsPrimary.ShouldBeTrue();
    }

    [Fact]
    public void PrimaryImage_WithNoPrimary_ShouldReturnFirst()
    {
        // Arrange
        var product = CreateTestProduct();
        var first = product.AddImage("https://example.com/1.jpg");
        product.AddImage("https://example.com/2.jpg");

        // Assert
        product.PrimaryImage.ShouldBe(first);
    }

    #endregion

    #region Option Tests

    [Fact]
    public void AddOption_ShouldAddToCollection()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        var option = product.AddOption("Color", "Color");

        // Assert
        product.Options.ShouldHaveSingleItem();
        option.Name.ShouldBe("color");
        option.DisplayName.ShouldBe("Color");
        option.SortOrder.ShouldBe(0);
    }

    [Fact]
    public void AddOption_ShouldIncrementSortOrder()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.AddOption("Color");
        var second = product.AddOption("Size");

        // Assert
        second.SortOrder.ShouldBe(1);
    }

    [Fact]
    public void RemoveOption_ShouldRemoveFromCollection()
    {
        // Arrange
        var product = CreateTestProduct();
        var option = product.AddOption("Color");

        // Act
        product.RemoveOption(option.Id);

        // Assert
        product.Options.ShouldBeEmpty();
    }

    [Fact]
    public void HasOptions_ShouldReflectCollection()
    {
        // Arrange
        var product = CreateTestProduct();
        product.HasOptions.ShouldBeFalse();

        // Act
        product.AddOption("Size");

        // Assert
        product.HasOptions.ShouldBeTrue();
    }

    #endregion

    #region Variant Detail Tests

    [Fact]
    public void Variant_UpdateDetails_ShouldUpdateFields()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("Original", 100m, "OLD-SKU");

        // Act
        variant.UpdateDetails("Updated", 200m, "NEW-SKU");

        // Assert
        variant.Name.ShouldBe("Updated");
        variant.Price.ShouldBe(200m);
        variant.Sku.ShouldBe("NEW-SKU");
    }

    [Fact]
    public void Variant_SetCompareAtPrice_ShouldEnableOnSale()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);

        // Act
        variant.SetCompareAtPrice(150m);

        // Assert
        variant.CompareAtPrice.ShouldBe(150m);
        variant.OnSale.ShouldBeTrue();
    }

    [Fact]
    public void Variant_OnSale_WhenCompareAtPriceLower_ShouldBeFalse()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);

        // Act
        variant.SetCompareAtPrice(50m);

        // Assert
        variant.OnSale.ShouldBeFalse();
    }

    [Fact]
    public void Variant_SetCostPrice_ShouldSetField()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);

        // Act
        variant.SetCostPrice(50m);

        // Assert
        variant.CostPrice.ShouldBe(50m);
    }

    [Fact]
    public void Variant_ReserveStock_ShouldDecrementQuantity()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);
        variant.SetStock(10);

        // Act
        variant.ReserveStock(3);

        // Assert
        variant.StockQuantity.ShouldBe(7);
    }

    [Fact]
    public void Variant_ReserveStock_InsufficientStock_ShouldThrow()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);
        variant.SetStock(2);

        // Act & Assert
        var act = () => variant.ReserveStock(5);
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Insufficient stock");
    }

    [Fact]
    public void Variant_ReleaseStock_ShouldIncrementQuantity()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);
        variant.SetStock(5);

        // Act
        variant.ReleaseStock(3);

        // Assert
        variant.StockQuantity.ShouldBe(8);
    }

    [Fact]
    public void Variant_AdjustStock_Positive_ShouldIncrement()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);
        variant.SetStock(5);

        // Act
        variant.AdjustStock(10);

        // Assert
        variant.StockQuantity.ShouldBe(15);
    }

    [Fact]
    public void Variant_AdjustStock_Negative_BelowZero_ShouldThrow()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);
        variant.SetStock(3);

        // Act & Assert
        var act = () => variant.AdjustStock(-5);
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Stock cannot be negative");
    }

    [Fact]
    public void Variant_SetStock_WithNegative_ShouldThrow()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);

        // Act & Assert
        var act = () => variant.SetStock(-1);
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Stock cannot be negative");
    }

    [Fact]
    public void Variant_InStock_ShouldReflectQuantity()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);

        // Assert
        variant.InStock.ShouldBeFalse();

        // Act
        variant.SetStock(1);

        // Assert
        variant.InStock.ShouldBeTrue();
    }

    [Fact]
    public void Variant_LowStock_ShouldBeTrueWhenAtOrBelowThreshold()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);

        variant.SetStock(5);
        variant.LowStock.ShouldBeTrue();

        variant.SetStock(6);
        variant.LowStock.ShouldBeFalse();

        variant.SetStock(0);
        variant.LowStock.ShouldBeFalse(); // 0 is not low stock, it's out of stock
    }

    [Fact]
    public void Variant_GetOptions_WithNullJson_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);

        // Assert
        variant.GetOptions().ShouldBeEmpty();
    }

    [Fact]
    public void Variant_UpdateOptions_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);
        var options = new Dictionary<string, string> { { "color", "Blue" } };

        // Act
        variant.UpdateOptions(options);

        // Assert
        variant.GetOptions().ShouldContainKeyAndValue("color", "Blue");
    }

    [Fact]
    public void Variant_SetSortOrder_ShouldUpdateValue()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);

        // Act
        variant.SetSortOrder(5);

        // Assert
        variant.SortOrder.ShouldBe(5);
    }

    [Fact]
    public void Variant_SetImage_ShouldSetImageId()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);
        var imageId = Guid.NewGuid();

        // Act
        variant.SetImage(imageId);

        // Assert
        variant.ImageId.ShouldBe(imageId);
    }

    #endregion

    #region Option Value Tests

    [Fact]
    public void Option_AddValue_ShouldAddToCollection()
    {
        // Arrange
        var product = CreateTestProduct();
        var option = product.AddOption("Color");

        // Act
        var value = option.AddValue("red", "Red");

        // Assert
        option.Values.ShouldHaveSingleItem();
        value.Value.ShouldBe("red");
        value.DisplayValue.ShouldBe("Red");
        value.SortOrder.ShouldBe(0);
    }

    [Fact]
    public void Option_AddValue_ShouldIncrementSortOrder()
    {
        // Arrange
        var product = CreateTestProduct();
        var option = product.AddOption("Color");

        // Act
        option.AddValue("red", "Red");
        var second = option.AddValue("blue", "Blue");

        // Assert
        second.SortOrder.ShouldBe(1);
    }

    [Fact]
    public void Option_RemoveValue_ShouldRemoveFromCollection()
    {
        // Arrange
        var product = CreateTestProduct();
        var option = product.AddOption("Color");
        var value = option.AddValue("red", "Red");

        // Act
        option.RemoveValue(value.Id);

        // Assert
        option.Values.ShouldBeEmpty();
    }

    [Fact]
    public void Option_Update_ShouldNormalizeNameAndSetDisplayName()
    {
        // Arrange
        var product = CreateTestProduct();
        var option = product.AddOption("Color");

        // Act
        option.Update("Shoe Size", "Shoe Size", 2);

        // Assert
        option.Name.ShouldBe("shoe_size");
        option.DisplayName.ShouldBe("Shoe Size");
        option.SortOrder.ShouldBe(2);
    }

    [Fact]
    public void Option_Create_WithNullDisplayName_ShouldUseNameAsDisplay()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        var option = product.AddOption("Color");

        // Assert
        option.DisplayName.ShouldBe("Color");
    }

    #endregion

    #region Image Detail Tests

    [Fact]
    public void Image_Update_ShouldChangeUrlAndAltText()
    {
        // Arrange
        var product = CreateTestProduct();
        var image = product.AddImage("https://old.com/img.jpg", "Old alt");

        // Act
        image.Update("https://new.com/img.jpg", "New alt");

        // Assert
        image.Url.ShouldBe("https://new.com/img.jpg");
        image.AltText.ShouldBe("New alt");
    }

    [Fact]
    public void Image_SetSortOrder_ShouldUpdateValue()
    {
        // Arrange
        var product = CreateTestProduct();
        var image = product.AddImage("https://example.com/img.jpg");

        // Act
        image.SetSortOrder(3);

        // Assert
        image.SortOrder.ShouldBe(3);
    }

    [Fact]
    public void Image_SetAsPrimary_ShouldSetFlag()
    {
        // Arrange
        var product = CreateTestProduct();
        var image = product.AddImage("https://example.com/img.jpg");
        image.IsPrimary.ShouldBeFalse();

        // Act
        image.SetAsPrimary();

        // Assert
        image.IsPrimary.ShouldBeTrue();
    }

    [Fact]
    public void Image_ClearPrimary_ShouldClearFlag()
    {
        // Arrange
        var product = CreateTestProduct();
        var image = product.AddImage("https://example.com/img.jpg", isPrimary: true);

        // Act
        image.ClearPrimary();

        // Assert
        image.IsPrimary.ShouldBeFalse();
    }

    #endregion
}
