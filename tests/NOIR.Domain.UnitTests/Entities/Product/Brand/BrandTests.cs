using NOIR.Domain.Entities.Product;
using NOIR.Domain.Events.Product;

namespace NOIR.Domain.UnitTests.Entities.Product.Brand;

/// <summary>
/// Unit tests for the Brand entity.
/// Tests factory methods, update methods, domain events, branding, SEO,
/// product count management, and status toggling.
/// </summary>
public class BrandTests
{
    private const string TestTenantId = "test-tenant";

    #region Helper Methods

    private static Domain.Entities.Product.Brand CreateTestBrand(
        string name = "Nike",
        string slug = "nike",
        string? tenantId = TestTenantId)
    {
        return Domain.Entities.Product.Brand.Create(name, slug, tenantId);
    }

    #endregion

    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidBrand()
    {
        // Act
        var brand = CreateTestBrand();

        // Assert
        brand.ShouldNotBeNull();
        brand.Id.ShouldNotBe(Guid.Empty);
        brand.Name.ShouldBe("Nike");
        brand.Slug.ShouldBe("nike");
        brand.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        // Act
        var brand = CreateTestBrand();

        // Assert
        brand.IsActive.ShouldBeTrue();
        brand.IsFeatured.ShouldBeFalse();
        brand.SortOrder.ShouldBe(0);
        brand.ProductCount.ShouldBe(0);
        brand.LogoUrl.ShouldBeNull();
        brand.BannerUrl.ShouldBeNull();
        brand.Description.ShouldBeNull();
        brand.Website.ShouldBeNull();
        brand.MetaTitle.ShouldBeNull();
        brand.MetaDescription.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldLowercaseSlug()
    {
        // Act
        var brand = Domain.Entities.Product.Brand.Create("Nike", "NIKE-Brand");

        // Assert
        brand.Slug.ShouldBe("nike-brand");
    }

    [Fact]
    public void Create_ShouldRaiseBrandCreatedEvent()
    {
        // Act
        var brand = CreateTestBrand();

        // Assert
        brand.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<BrandCreatedEvent>()
            .BrandId.ShouldBe(brand.Id);
    }

    [Fact]
    public void Create_ShouldRaiseBrandCreatedEventWithCorrectData()
    {
        // Act
        var brand = CreateTestBrand(name: "Adidas", slug: "adidas");

        // Assert
        var domainEvent = brand.DomainEvents.Single() as BrandCreatedEvent;
        domainEvent!.Name.ShouldBe("Adidas");
        domainEvent.Slug.ShouldBe("adidas");
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var brand = Domain.Entities.Product.Brand.Create("Global Brand", "global-brand", null);

        // Assert
        brand.TenantId.ShouldBeNull();
    }

    #endregion

    #region UpdateDetails Tests

    [Fact]
    public void UpdateDetails_ShouldUpdateAllFields()
    {
        // Arrange
        var brand = CreateTestBrand();
        brand.ClearDomainEvents();

        // Act
        brand.UpdateDetails("Adidas", "adidas", "Sportswear brand", "https://adidas.com");

        // Assert
        brand.Name.ShouldBe("Adidas");
        brand.Slug.ShouldBe("adidas");
        brand.Description.ShouldBe("Sportswear brand");
        brand.Website.ShouldBe("https://adidas.com");
    }

    [Fact]
    public void UpdateDetails_ShouldLowercaseSlug()
    {
        // Arrange
        var brand = CreateTestBrand();

        // Act
        brand.UpdateDetails("Adidas", "ADIDAS-Brand", null, null);

        // Assert
        brand.Slug.ShouldBe("adidas-brand");
    }

    [Fact]
    public void UpdateDetails_ShouldRaiseBrandUpdatedEvent()
    {
        // Arrange
        var brand = CreateTestBrand();
        brand.ClearDomainEvents();

        // Act
        brand.UpdateDetails("Updated", "updated", null, null);

        // Assert
        brand.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<BrandUpdatedEvent>()
            .Name.ShouldBe("Updated");
    }

    [Fact]
    public void UpdateDetails_WithNullDescriptionAndWebsite_ShouldSetNulls()
    {
        // Arrange
        var brand = CreateTestBrand();
        brand.UpdateDetails("Brand", "brand", "Some desc", "https://example.com");

        // Act
        brand.UpdateDetails("Brand", "brand", null, null);

        // Assert
        brand.Description.ShouldBeNull();
        brand.Website.ShouldBeNull();
    }

    #endregion

    #region UpdateBranding Tests

    [Fact]
    public void UpdateBranding_ShouldSetLogoAndBanner()
    {
        // Arrange
        var brand = CreateTestBrand();

        // Act
        brand.UpdateBranding("https://logo.png", "https://banner.jpg");

        // Assert
        brand.LogoUrl.ShouldBe("https://logo.png");
        brand.BannerUrl.ShouldBe("https://banner.jpg");
    }

    [Fact]
    public void UpdateBranding_WithNullValues_ShouldClearBranding()
    {
        // Arrange
        var brand = CreateTestBrand();
        brand.UpdateBranding("https://logo.png", "https://banner.jpg");

        // Act
        brand.UpdateBranding(null, null);

        // Assert
        brand.LogoUrl.ShouldBeNull();
        brand.BannerUrl.ShouldBeNull();
    }

    #endregion

    #region UpdateSeo Tests

    [Fact]
    public void UpdateSeo_ShouldSetMetaFields()
    {
        // Arrange
        var brand = CreateTestBrand();

        // Act
        brand.UpdateSeo("Nike - Best Shoes", "Official Nike brand page");

        // Assert
        brand.MetaTitle.ShouldBe("Nike - Best Shoes");
        brand.MetaDescription.ShouldBe("Official Nike brand page");
    }

    [Fact]
    public void UpdateSeo_WithNullValues_ShouldClearSeo()
    {
        // Arrange
        var brand = CreateTestBrand();
        brand.UpdateSeo("Title", "Description");

        // Act
        brand.UpdateSeo(null, null);

        // Assert
        brand.MetaTitle.ShouldBeNull();
        brand.MetaDescription.ShouldBeNull();
    }

    #endregion

    #region SetFeatured Tests

    [Fact]
    public void SetFeatured_True_ShouldSetIsFeatured()
    {
        // Arrange
        var brand = CreateTestBrand();

        // Act
        brand.SetFeatured(true);

        // Assert
        brand.IsFeatured.ShouldBeTrue();
    }

    [Fact]
    public void SetFeatured_False_ShouldClearIsFeatured()
    {
        // Arrange
        var brand = CreateTestBrand();
        brand.SetFeatured(true);

        // Act
        brand.SetFeatured(false);

        // Assert
        brand.IsFeatured.ShouldBeFalse();
    }

    #endregion

    #region SetActive Tests

    [Fact]
    public void SetActive_False_ShouldDeactivateBrand()
    {
        // Arrange
        var brand = CreateTestBrand();
        brand.IsActive.ShouldBeTrue();

        // Act
        brand.SetActive(false);

        // Assert
        brand.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void SetActive_True_ShouldReactivateBrand()
    {
        // Arrange
        var brand = CreateTestBrand();
        brand.SetActive(false);

        // Act
        brand.SetActive(true);

        // Assert
        brand.IsActive.ShouldBeTrue();
    }

    #endregion

    #region ProductCount Tests

    [Fact]
    public void UpdateProductCount_ShouldSetCount()
    {
        // Arrange
        var brand = CreateTestBrand();

        // Act
        brand.UpdateProductCount(42);

        // Assert
        brand.ProductCount.ShouldBe(42);
    }

    [Fact]
    public void IncrementProductCount_ShouldIncrementByOne()
    {
        // Arrange
        var brand = CreateTestBrand();

        // Act
        brand.IncrementProductCount();
        brand.IncrementProductCount();
        brand.IncrementProductCount();

        // Assert
        brand.ProductCount.ShouldBe(3);
    }

    [Fact]
    public void DecrementProductCount_ShouldDecrementByOne()
    {
        // Arrange
        var brand = CreateTestBrand();
        brand.UpdateProductCount(5);

        // Act
        brand.DecrementProductCount();

        // Assert
        brand.ProductCount.ShouldBe(4);
    }

    [Fact]
    public void DecrementProductCount_AtZero_ShouldNotGoBelowZero()
    {
        // Arrange
        var brand = CreateTestBrand();
        brand.ProductCount.ShouldBe(0);

        // Act
        brand.DecrementProductCount();

        // Assert
        brand.ProductCount.ShouldBe(0);
    }

    #endregion

    #region SetSortOrder Tests

    [Fact]
    public void SetSortOrder_ShouldUpdateValue()
    {
        // Arrange
        var brand = CreateTestBrand();

        // Act
        brand.SetSortOrder(10);

        // Assert
        brand.SortOrder.ShouldBe(10);
    }

    #endregion

    #region MarkAsDeleted Tests

    [Fact]
    public void MarkAsDeleted_ShouldRaiseBrandDeletedEvent()
    {
        // Arrange
        var brand = CreateTestBrand();
        brand.ClearDomainEvents();

        // Act
        brand.MarkAsDeleted();

        // Assert
        brand.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<BrandDeletedEvent>()
            .BrandId.ShouldBe(brand.Id);
    }

    #endregion
}
