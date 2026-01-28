using NOIR.Application.Features.Products.Common;
using NOIR.Application.Features.Products.DTOs;

namespace NOIR.Application.UnitTests.Features.Products;

/// <summary>
/// Unit tests for ProductMapper.
/// Tests centralized DTO mapping logic for Product-related entities.
/// </summary>
public class ProductMapperTests
{
    #region Test Setup

    private const string TestTenantId = "test-tenant";

    private static Product CreateTestProduct(
        string name = "Test Product",
        string slug = "test-product",
        decimal basePrice = 99.99m,
        string currency = "VND")
    {
        return Product.Create(name, slug, basePrice, currency, TestTenantId);
    }

    private static ProductCategory CreateTestCategory(
        string name = "Test Category",
        string slug = "test-category",
        Guid? parentId = null)
    {
        return ProductCategory.Create(name, slug, parentId, TestTenantId);
    }

    #endregion

    #region Product ToDto Tests

    [Fact]
    public void ToDto_WithExplicitCategoryInfo_MapsAllProperties()
    {
        // Arrange
        var product = CreateTestProduct();
        product.UpdateBasicInfo("Test Product", "test-product", "Short desc", "Description", "<p>HTML</p>");
        product.SetBrand("TestBrand");
        product.UpdateIdentification("SKU-001", "BARCODE-001");
        product.SetWeight(1.5m);
        product.SetInventoryTracking(true);
        product.UpdateSeo("Meta Title", "Meta Description");

        var categoryName = "Electronics";
        var categorySlug = "electronics";
        var variants = new List<ProductVariantDto>();
        var images = new List<ProductImageDto>();

        // Act
        var dto = ProductMapper.ToDto(product, categoryName, categorySlug, variants, images);

        // Assert
        dto.Id.Should().Be(product.Id);
        dto.Name.Should().Be("Test Product");
        dto.Slug.Should().Be("test-product");
        dto.Description.Should().Be("Description");
        dto.DescriptionHtml.Should().Be("<p>HTML</p>");
        dto.BasePrice.Should().Be(99.99m);
        dto.Currency.Should().Be("VND");
        dto.CategoryName.Should().Be(categoryName);
        dto.CategorySlug.Should().Be(categorySlug);
        dto.Brand.Should().Be("TestBrand");
        dto.Sku.Should().Be("SKU-001");
        dto.Barcode.Should().Be("BARCODE-001");
        dto.Weight.Should().Be(1.5m);
        dto.TrackInventory.Should().BeTrue();
        dto.MetaTitle.Should().Be("Meta Title");
        dto.MetaDescription.Should().Be("Meta Description");
        dto.Variants.Should().BeSameAs(variants);
        dto.Images.Should().BeSameAs(images);
    }

    [Fact]
    public void ToDto_WithNullCategoryInfo_HandlesGracefully()
    {
        // Arrange
        var product = CreateTestProduct();
        var variants = new List<ProductVariantDto>();
        var images = new List<ProductImageDto>();

        // Act
        var dto = ProductMapper.ToDto(product, null, null, variants, images);

        // Assert
        dto.CategoryId.Should().BeNull();
        dto.CategoryName.Should().BeNull();
        dto.CategorySlug.Should().BeNull();
    }

    [Fact]
    public void ToDto_WithNavigationProperty_MapsFromCategory()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("Default", 99.99m);
        var image = product.AddImage("https://example.com/img.jpg", "Alt text", true);

        // Act
        var dto = ProductMapper.ToDto(product);

        // Assert
        dto.Id.Should().Be(product.Id);
        dto.Variants.Should().HaveCount(1);
        dto.Variants[0].Name.Should().Be("Default");
        dto.Images.Should().HaveCount(1);
        dto.Images[0].Url.Should().Be("https://example.com/img.jpg");
    }

    [Fact]
    public void ToDto_VariantCollections_AreSortedBySortOrder()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant3 = product.AddVariant("Third", 30m);
        variant3.SetSortOrder(3);
        var variant1 = product.AddVariant("First", 10m);
        variant1.SetSortOrder(1);
        var variant2 = product.AddVariant("Second", 20m);
        variant2.SetSortOrder(2);

        // Act
        var dto = ProductMapper.ToDto(product);

        // Assert
        dto.Variants.Should().HaveCount(3);
        dto.Variants[0].Name.Should().Be("First");
        dto.Variants[1].Name.Should().Be("Second");
        dto.Variants[2].Name.Should().Be("Third");
    }

    [Fact]
    public void ToDto_ImageCollections_AreSortedBySortOrder()
    {
        // Arrange
        var product = CreateTestProduct();
        var image3 = product.AddImage("https://example.com/3.jpg", "Third", false);
        image3.SetSortOrder(3);
        var image1 = product.AddImage("https://example.com/1.jpg", "First", true);
        image1.SetSortOrder(1);
        var image2 = product.AddImage("https://example.com/2.jpg", "Second", false);
        image2.SetSortOrder(2);

        // Act
        var dto = ProductMapper.ToDto(product);

        // Assert
        dto.Images.Should().HaveCount(3);
        dto.Images[0].AltText.Should().Be("First");
        dto.Images[1].AltText.Should().Be("Second");
        dto.Images[2].AltText.Should().Be("Third");
    }

    [Fact]
    public void ToDtoWithCollections_AutomaticallyMapsVariantsAndImages()
    {
        // Arrange
        var product = CreateTestProduct();
        product.AddVariant("Variant 1", 10m);
        product.AddVariant("Variant 2", 20m);
        product.AddImage("https://example.com/img1.jpg", "Image 1", true);

        // Act
        var dto = ProductMapper.ToDtoWithCollections(product, "Category", "category-slug");

        // Assert
        dto.CategoryName.Should().Be("Category");
        dto.CategorySlug.Should().Be("category-slug");
        dto.Variants.Should().HaveCount(2);
        dto.Images.Should().HaveCount(1);
    }

    #endregion

    #region Product ToListDto Tests

    [Fact]
    public void ToListDto_MapsBasicProperties()
    {
        // Arrange
        var product = CreateTestProduct("List Product", "list-product", 199.99m, "USD");
        product.SetBrand("ListBrand");
        product.UpdateIdentification("LIST-SKU", null);

        // Act
        var dto = ProductMapper.ToListDto(product);

        // Assert
        dto.Id.Should().Be(product.Id);
        dto.Name.Should().Be("List Product");
        dto.Slug.Should().Be("list-product");
        dto.BasePrice.Should().Be(199.99m);
        dto.Currency.Should().Be("USD");
        dto.Brand.Should().Be("ListBrand");
        dto.Sku.Should().Be("LIST-SKU");
    }

    [Fact]
    public void ToListDto_SelectsPrimaryImage()
    {
        // Arrange
        var product = CreateTestProduct();
        product.AddImage("https://example.com/secondary.jpg", "Secondary", false);
        product.AddImage("https://example.com/primary.jpg", "Primary", true);

        // Act
        var dto = ProductMapper.ToListDto(product);

        // Assert
        dto.PrimaryImageUrl.Should().Be("https://example.com/primary.jpg");
    }

    [Fact]
    public void ToListDto_FallsBackToFirstImage_WhenNoPrimary()
    {
        // Arrange
        var product = CreateTestProduct();
        product.AddImage("https://example.com/first.jpg", "First", false);
        product.AddImage("https://example.com/second.jpg", "Second", false);

        // Act
        var dto = ProductMapper.ToListDto(product);

        // Assert
        dto.PrimaryImageUrl.Should().Be("https://example.com/first.jpg");
    }

    [Fact]
    public void ToListDto_ReturnsNullImageUrl_WhenNoImages()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        var dto = ProductMapper.ToListDto(product);

        // Assert
        dto.PrimaryImageUrl.Should().BeNull();
    }

    #endregion

    #region ProductVariant ToDto Tests

    [Fact]
    public void ToDto_Variant_MapsAllProperties()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("Large Red", 149.99m, "VAR-001", new Dictionary<string, string>
        {
            { "Size", "Large" },
            { "Color", "Red" }
        });
        variant.SetCompareAtPrice(199.99m);
        variant.SetStock(50);
        variant.SetSortOrder(2);

        // Act
        var dto = ProductMapper.ToDto(variant);

        // Assert
        dto.Id.Should().Be(variant.Id);
        dto.Name.Should().Be("Large Red");
        dto.Sku.Should().Be("VAR-001");
        dto.Price.Should().Be(149.99m);
        dto.CompareAtPrice.Should().Be(199.99m);
        dto.StockQuantity.Should().Be(50);
        dto.InStock.Should().BeTrue();
        dto.OnSale.Should().BeTrue();
        dto.SortOrder.Should().Be(2);
        dto.Options.Should().ContainKey("Size").WhoseValue.Should().Be("Large");
        dto.Options.Should().ContainKey("Color").WhoseValue.Should().Be("Red");
    }

    [Fact]
    public void ToDto_Variant_LowStock_IndicatesCorrectly()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("Low Stock Variant", 10m);
        variant.SetStock(3); // Assuming low stock threshold is 5

        // Act
        var dto = ProductMapper.ToDto(variant);

        // Assert
        dto.InStock.Should().BeTrue();
        dto.LowStock.Should().BeTrue();
    }

    [Fact]
    public void ToDto_Variant_OutOfStock()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("Out of Stock", 10m);
        variant.SetStock(0);

        // Act
        var dto = ProductMapper.ToDto(variant);

        // Assert
        dto.InStock.Should().BeFalse();
        dto.StockQuantity.Should().Be(0);
    }

    #endregion

    #region ProductImage ToDto Tests

    [Fact]
    public void ToDto_Image_MapsAllProperties()
    {
        // Arrange
        var product = CreateTestProduct();
        var image = product.AddImage("https://cdn.example.com/product/image.jpg", "Product image", true);
        image.SetSortOrder(1);

        // Act
        var dto = ProductMapper.ToDto(image);

        // Assert
        dto.Id.Should().Be(image.Id);
        dto.Url.Should().Be("https://cdn.example.com/product/image.jpg");
        dto.AltText.Should().Be("Product image");
        dto.IsPrimary.Should().BeTrue();
        dto.SortOrder.Should().Be(1);
    }

    [Fact]
    public void ToDto_Image_WithNullAltText()
    {
        // Arrange
        var product = CreateTestProduct();
        var image = product.AddImage("https://example.com/img.jpg", null, false);

        // Act
        var dto = ProductMapper.ToDto(image);

        // Assert
        dto.AltText.Should().BeNull();
        dto.IsPrimary.Should().BeFalse();
    }

    #endregion

    #region ProductCategory ToDto Tests

    [Fact]
    public void ToDto_Category_WithExplicitParentName()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var category = CreateTestCategory("Child Category", "child-category", parentId);
        category.UpdateDetails("Child Category", "child-category", "A child category", "https://example.com/img.jpg");
        category.UpdateSeo("Child Meta", "Child Meta Description");
        category.SetSortOrder(5);

        // Act
        var dto = ProductMapper.ToDto(category, "Parent Category");

        // Assert
        dto.Id.Should().Be(category.Id);
        dto.Name.Should().Be("Child Category");
        dto.Slug.Should().Be("child-category");
        dto.Description.Should().Be("A child category");
        dto.ImageUrl.Should().Be("https://example.com/img.jpg");
        dto.MetaTitle.Should().Be("Child Meta");
        dto.MetaDescription.Should().Be("Child Meta Description");
        dto.SortOrder.Should().Be(5);
        dto.ParentId.Should().Be(parentId);
        dto.ParentName.Should().Be("Parent Category");
        dto.Children.Should().BeNull(); // Children not loaded in command context
    }

    [Fact]
    public void ToDto_Category_WithNullParent()
    {
        // Arrange
        var category = CreateTestCategory("Root Category", "root-category");

        // Act
        var dto = ProductMapper.ToDto(category, null);

        // Assert
        dto.ParentId.Should().BeNull();
        dto.ParentName.Should().BeNull();
    }

    [Fact]
    public void ToDtoWithChildren_MapsChildCategories()
    {
        // Arrange
        var parent = CreateTestCategory("Parent", "parent");
        var child1 = ProductMapper.ToDto(CreateTestCategory("Child 1", "child-1", parent.Id), "Parent");
        var child2 = ProductMapper.ToDto(CreateTestCategory("Child 2", "child-2", parent.Id), "Parent");
        var children = new List<ProductCategoryDto> { child1, child2 };

        // Act
        var dto = ProductMapper.ToDtoWithChildren(parent, children);

        // Assert
        dto.Children.Should().HaveCount(2);
        dto.Children.Should().Contain(c => c.Name == "Child 1");
        dto.Children.Should().Contain(c => c.Name == "Child 2");
    }

    [Fact]
    public void ToDtoWithChildren_HandlesNullChildren()
    {
        // Arrange
        var category = CreateTestCategory();

        // Act
        var dto = ProductMapper.ToDtoWithChildren(category, null);

        // Assert
        dto.Children.Should().BeNull();
    }

    #endregion

    #region ProductCategory ToListDto Tests

    [Fact]
    public void ToListDto_Category_MapsBasicProperties()
    {
        // Arrange
        var category = CreateTestCategory("List Category", "list-category");
        category.UpdateDetails("List Category", "list-category", "Description", null);
        category.SetSortOrder(10);

        // Act
        var dto = ProductMapper.ToListDto(category);

        // Assert
        dto.Id.Should().Be(category.Id);
        dto.Name.Should().Be("List Category");
        dto.Slug.Should().Be("list-category");
        dto.Description.Should().Be("Description");
        dto.SortOrder.Should().Be(10);
        dto.ProductCount.Should().Be(0);
        dto.ParentId.Should().BeNull();
        dto.ParentName.Should().BeNull();
        dto.ChildCount.Should().Be(0);
    }

    #endregion
}
