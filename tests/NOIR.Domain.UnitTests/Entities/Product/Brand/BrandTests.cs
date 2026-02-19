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
        brand.Should().NotBeNull();
        brand.Id.Should().NotBe(Guid.Empty);
        brand.Name.Should().Be("Nike");
        brand.Slug.Should().Be("nike");
        brand.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        // Act
        var brand = CreateTestBrand();

        // Assert
        brand.IsActive.Should().BeTrue();
        brand.IsFeatured.Should().BeFalse();
        brand.SortOrder.Should().Be(0);
        brand.ProductCount.Should().Be(0);
        brand.LogoUrl.Should().BeNull();
        brand.BannerUrl.Should().BeNull();
        brand.Description.Should().BeNull();
        brand.Website.Should().BeNull();
        brand.MetaTitle.Should().BeNull();
        brand.MetaDescription.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldLowercaseSlug()
    {
        // Act
        var brand = Domain.Entities.Product.Brand.Create("Nike", "NIKE-Brand");

        // Assert
        brand.Slug.Should().Be("nike-brand");
    }

    [Fact]
    public void Create_ShouldRaiseBrandCreatedEvent()
    {
        // Act
        var brand = CreateTestBrand();

        // Assert
        brand.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BrandCreatedEvent>()
            .Which.BrandId.Should().Be(brand.Id);
    }

    [Fact]
    public void Create_ShouldRaiseBrandCreatedEventWithCorrectData()
    {
        // Act
        var brand = CreateTestBrand(name: "Adidas", slug: "adidas");

        // Assert
        var domainEvent = brand.DomainEvents.Single() as BrandCreatedEvent;
        domainEvent!.Name.Should().Be("Adidas");
        domainEvent.Slug.Should().Be("adidas");
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var brand = Domain.Entities.Product.Brand.Create("Global Brand", "global-brand", null);

        // Assert
        brand.TenantId.Should().BeNull();
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
        brand.Name.Should().Be("Adidas");
        brand.Slug.Should().Be("adidas");
        brand.Description.Should().Be("Sportswear brand");
        brand.Website.Should().Be("https://adidas.com");
    }

    [Fact]
    public void UpdateDetails_ShouldLowercaseSlug()
    {
        // Arrange
        var brand = CreateTestBrand();

        // Act
        brand.UpdateDetails("Adidas", "ADIDAS-Brand", null, null);

        // Assert
        brand.Slug.Should().Be("adidas-brand");
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
        brand.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BrandUpdatedEvent>()
            .Which.Name.Should().Be("Updated");
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
        brand.Description.Should().BeNull();
        brand.Website.Should().BeNull();
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
        brand.LogoUrl.Should().Be("https://logo.png");
        brand.BannerUrl.Should().Be("https://banner.jpg");
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
        brand.LogoUrl.Should().BeNull();
        brand.BannerUrl.Should().BeNull();
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
        brand.MetaTitle.Should().Be("Nike - Best Shoes");
        brand.MetaDescription.Should().Be("Official Nike brand page");
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
        brand.MetaTitle.Should().BeNull();
        brand.MetaDescription.Should().BeNull();
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
        brand.IsFeatured.Should().BeTrue();
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
        brand.IsFeatured.Should().BeFalse();
    }

    #endregion

    #region SetActive Tests

    [Fact]
    public void SetActive_False_ShouldDeactivateBrand()
    {
        // Arrange
        var brand = CreateTestBrand();
        brand.IsActive.Should().BeTrue();

        // Act
        brand.SetActive(false);

        // Assert
        brand.IsActive.Should().BeFalse();
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
        brand.IsActive.Should().BeTrue();
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
        brand.ProductCount.Should().Be(42);
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
        brand.ProductCount.Should().Be(3);
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
        brand.ProductCount.Should().Be(4);
    }

    [Fact]
    public void DecrementProductCount_AtZero_ShouldNotGoBelowZero()
    {
        // Arrange
        var brand = CreateTestBrand();
        brand.ProductCount.Should().Be(0);

        // Act
        brand.DecrementProductCount();

        // Assert
        brand.ProductCount.Should().Be(0);
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
        brand.SortOrder.Should().Be(10);
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
        brand.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BrandDeletedEvent>()
            .Which.BrandId.Should().Be(brand.Id);
    }

    #endregion
}
