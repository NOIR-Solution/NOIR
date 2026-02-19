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
        product.Should().NotBeNull();
        product.Id.Should().NotBe(Guid.Empty);
        product.Name.Should().Be("Test Product");
        product.Slug.Should().Be("test-product");
        product.BasePrice.Should().Be(100_000m);
        product.Currency.Should().Be("VND");
        product.Status.Should().Be(ProductStatus.Draft);
        product.TrackInventory.Should().BeTrue();
        product.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldLowercaseSlug()
    {
        // Act
        var product = Domain.Entities.Product.Product.Create("Test", "My-PRODUCT-Slug", 100m);

        // Assert
        product.Slug.Should().Be("my-product-slug");
    }

    [Fact]
    public void Create_ShouldRaiseDomainEvent()
    {
        // Act
        var product = CreateTestProduct();

        // Assert
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCreatedEvent>()
            .Which.ProductId.Should().Be(product.Id);
    }

    [Fact]
    public void Create_WithDefaultCurrency_ShouldUseVND()
    {
        // Act
        var product = Domain.Entities.Product.Product.Create("Test", "test", 100m);

        // Assert
        product.Currency.Should().Be("VND");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrow(string? name)
    {
        // Act & Assert
        var act = () => Domain.Entities.Product.Product.Create(name!, "slug", 100m);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidSlug_ShouldThrow(string? slug)
    {
        // Act & Assert
        var act = () => Domain.Entities.Product.Product.Create("Name", slug!, 100m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNegativePrice_ShouldThrow()
    {
        // Act & Assert
        var act = () => Domain.Entities.Product.Product.Create("Name", "slug", -1m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithZeroPrice_ShouldSucceed()
    {
        // Act
        var product = Domain.Entities.Product.Product.Create("Free Product", "free", 0m);

        // Assert
        product.BasePrice.Should().Be(0m);
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
        product.Name.Should().Be("New Name");
        product.Slug.Should().Be("new-slug");
        product.ShortDescription.Should().Be("Short desc");
        product.Description.Should().Be("Full desc");
        product.DescriptionHtml.Should().Be("<p>HTML</p>");
    }

    [Fact]
    public void UpdateBasicInfo_ShouldLowercaseSlug()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.UpdateBasicInfo("Name", "NEW-Slug", null, null, null);

        // Assert
        product.Slug.Should().Be("new-slug");
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
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductUpdatedEvent>();
    }

    [Fact]
    public void UpdateBasicInfo_ShouldTrimShortDescription()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.UpdateBasicInfo("Name", "slug", "  trimmed  ", null, null);

        // Assert
        product.ShortDescription.Should().Be("trimmed");
    }

    [Fact]
    public void UpdatePricing_ShouldUpdatePriceAndCurrency()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.UpdatePricing(200_000m, "USD");

        // Assert
        product.BasePrice.Should().Be(200_000m);
        product.Currency.Should().Be("USD");
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
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductUpdatedEvent>();
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
        product.CategoryId.Should().Be(categoryId);
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
        product.CategoryId.Should().BeNull();
    }

    [Fact]
    public void SetBrand_ShouldSetBrandString()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.SetBrand("Nike");

        // Assert
        product.Brand.Should().Be("Nike");
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
        product.BrandId.Should().Be(brandId);
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductUpdatedEvent>();
    }

    [Fact]
    public void UpdateIdentification_ShouldSetSkuAndBarcode()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.UpdateIdentification("SKU-001", "BARCODE-001");

        // Assert
        product.Sku.Should().Be("SKU-001");
        product.Barcode.Should().Be("BARCODE-001");
    }

    [Fact]
    public void UpdateIdentification_WithWhitespace_ShouldTrimOrNullify()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.UpdateIdentification("  SKU  ", "   ");

        // Assert
        product.Sku.Should().Be("SKU");
        product.Barcode.Should().BeNull();
    }

    [Fact]
    public void UpdateSeo_ShouldSetMetaFields()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.UpdateSeo("SEO Title", "SEO Description");

        // Assert
        product.MetaTitle.Should().Be("SEO Title");
        product.MetaDescription.Should().Be("SEO Description");
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
        product.Weight.Should().Be(1.5m);
        product.WeightUnit.Should().Be("kg");
        product.Length.Should().Be(30m);
        product.Width.Should().Be(20m);
        product.Height.Should().Be(10m);
        product.DimensionUnit.Should().Be("cm");
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductUpdatedEvent>();
    }

    [Fact]
    public void SetInventoryTracking_ShouldUpdateFlag()
    {
        // Arrange
        var product = CreateTestProduct();
        product.TrackInventory.Should().BeTrue();

        // Act
        product.SetInventoryTracking(false);

        // Assert
        product.TrackInventory.Should().BeFalse();
    }

    #endregion

    #region Status Transition Tests

    [Fact]
    public void Publish_WhenDraft_ShouldSetActive()
    {
        // Arrange
        var product = CreateTestProduct();
        product.Status.Should().Be(ProductStatus.Draft);
        product.ClearDomainEvents();

        // Act
        product.Publish();

        // Assert
        product.Status.Should().Be(ProductStatus.Active);
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductPublishedEvent>();
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
        product.Status.Should().Be(ProductStatus.Active);
        product.DomainEvents.Should().BeEmpty();
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
        product.Status.Should().Be(ProductStatus.Archived);
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductArchivedEvent>();
    }

    [Fact]
    public void SetOutOfStock_WhenNoVariants_ShouldSetStatus()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.SetOutOfStock();

        // Assert — TotalStock is 0 (no variants), so it should set OutOfStock
        product.Status.Should().Be(ProductStatus.OutOfStock);
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
        product.Status.Should().Be(ProductStatus.Active);
    }

    [Fact]
    public void RestoreFromOutOfStock_WhenHasStock_ShouldSetActive()
    {
        // Arrange
        var product = CreateTestProduct();
        product.SetOutOfStock();
        product.Status.Should().Be(ProductStatus.OutOfStock);

        var variant = product.AddVariant("Default", 100m, "SKU-1");
        variant.SetStock(5);

        // Act
        product.RestoreFromOutOfStock();

        // Assert
        product.Status.Should().Be(ProductStatus.Active);
    }

    [Fact]
    public void RestoreFromOutOfStock_WhenNotOutOfStock_ShouldNotChange()
    {
        // Arrange
        var product = CreateTestProduct();
        product.Status.Should().Be(ProductStatus.Draft);

        // Act
        product.RestoreFromOutOfStock();

        // Assert
        product.Status.Should().Be(ProductStatus.Draft);
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
        product.Variants.Should().ContainSingle();
        variant.Name.Should().Be("Size M");
        variant.Price.Should().Be(120_000m);
        variant.Sku.Should().Be("SKU-M");
        variant.ProductId.Should().Be(product.Id);
        variant.TenantId.Should().Be(TestTenantId);
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
        parsed.Should().ContainKey("color").WhoseValue.Should().Be("Red");
        parsed.Should().ContainKey("size").WhoseValue.Should().Be("M");
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
        product.Variants.Should().BeEmpty();
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
        product.Variants.Should().HaveCount(1);
    }

    [Fact]
    public void HasVariants_ShouldReflectCollection()
    {
        // Arrange
        var product = CreateTestProduct();
        product.HasVariants.Should().BeFalse();

        // Act
        product.AddVariant("V1", 100m);

        // Assert
        product.HasVariants.Should().BeTrue();
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
        product.TotalStock.Should().Be(15);
        product.InStock.Should().BeTrue();
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
        product.Images.Should().ContainSingle();
        image.Url.Should().Be("https://example.com/img.jpg");
        image.AltText.Should().Be("Alt text");
        image.SortOrder.Should().Be(0);
        image.IsPrimary.Should().BeFalse();
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
        first.IsPrimary.Should().BeFalse();
        second.IsPrimary.Should().BeTrue();
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
        first.SortOrder.Should().Be(0);
        second.SortOrder.Should().Be(1);
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
        product.Images.Should().BeEmpty();
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
        img1.IsPrimary.Should().BeFalse();
        img2.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void PrimaryImage_ShouldReturnFirstPrimary()
    {
        // Arrange
        var product = CreateTestProduct();
        product.AddImage("https://example.com/1.jpg");
        product.AddImage("https://example.com/2.jpg", isPrimary: true);

        // Assert
        product.PrimaryImage!.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void PrimaryImage_WithNoPrimary_ShouldReturnFirst()
    {
        // Arrange
        var product = CreateTestProduct();
        var first = product.AddImage("https://example.com/1.jpg");
        product.AddImage("https://example.com/2.jpg");

        // Assert
        product.PrimaryImage.Should().Be(first);
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
        product.Options.Should().ContainSingle();
        option.Name.Should().Be("color");
        option.DisplayName.Should().Be("Color");
        option.SortOrder.Should().Be(0);
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
        second.SortOrder.Should().Be(1);
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
        product.Options.Should().BeEmpty();
    }

    [Fact]
    public void HasOptions_ShouldReflectCollection()
    {
        // Arrange
        var product = CreateTestProduct();
        product.HasOptions.Should().BeFalse();

        // Act
        product.AddOption("Size");

        // Assert
        product.HasOptions.Should().BeTrue();
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
        variant.Name.Should().Be("Updated");
        variant.Price.Should().Be(200m);
        variant.Sku.Should().Be("NEW-SKU");
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
        variant.CompareAtPrice.Should().Be(150m);
        variant.OnSale.Should().BeTrue();
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
        variant.OnSale.Should().BeFalse();
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
        variant.CostPrice.Should().Be(50m);
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
        variant.StockQuantity.Should().Be(7);
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insufficient stock*");
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
        variant.StockQuantity.Should().Be(8);
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
        variant.StockQuantity.Should().Be(15);
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Stock cannot be negative*");
    }

    [Fact]
    public void Variant_SetStock_WithNegative_ShouldThrow()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);

        // Act & Assert
        var act = () => variant.SetStock(-1);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Stock cannot be negative*");
    }

    [Fact]
    public void Variant_InStock_ShouldReflectQuantity()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);

        // Assert
        variant.InStock.Should().BeFalse();

        // Act
        variant.SetStock(1);

        // Assert
        variant.InStock.Should().BeTrue();
    }

    [Fact]
    public void Variant_LowStock_ShouldBeTrueWhenAtOrBelowThreshold()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);

        variant.SetStock(5);
        variant.LowStock.Should().BeTrue();

        variant.SetStock(6);
        variant.LowStock.Should().BeFalse();

        variant.SetStock(0);
        variant.LowStock.Should().BeFalse(); // 0 is not low stock, it's out of stock
    }

    [Fact]
    public void Variant_GetOptions_WithNullJson_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("V1", 100m);

        // Assert
        variant.GetOptions().Should().BeEmpty();
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
        variant.GetOptions().Should().ContainKey("color").WhoseValue.Should().Be("Blue");
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
        variant.SortOrder.Should().Be(5);
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
        variant.ImageId.Should().Be(imageId);
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
        option.Values.Should().ContainSingle();
        value.Value.Should().Be("red");
        value.DisplayValue.Should().Be("Red");
        value.SortOrder.Should().Be(0);
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
        second.SortOrder.Should().Be(1);
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
        option.Values.Should().BeEmpty();
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
        option.Name.Should().Be("shoe_size");
        option.DisplayName.Should().Be("Shoe Size");
        option.SortOrder.Should().Be(2);
    }

    [Fact]
    public void Option_Create_WithNullDisplayName_ShouldUseNameAsDisplay()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        var option = product.AddOption("Color");

        // Assert
        option.DisplayName.Should().Be("Color");
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
        image.Url.Should().Be("https://new.com/img.jpg");
        image.AltText.Should().Be("New alt");
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
        image.SortOrder.Should().Be(3);
    }

    [Fact]
    public void Image_SetAsPrimary_ShouldSetFlag()
    {
        // Arrange
        var product = CreateTestProduct();
        var image = product.AddImage("https://example.com/img.jpg");
        image.IsPrimary.Should().BeFalse();

        // Act
        image.SetAsPrimary();

        // Assert
        image.IsPrimary.Should().BeTrue();
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
        image.IsPrimary.Should().BeFalse();
    }

    #endregion
}
