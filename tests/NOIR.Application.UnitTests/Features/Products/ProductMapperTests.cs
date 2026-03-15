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
        product.SetInventoryTracking(true);
        product.UpdateSeo("Meta Title", "Meta Description");

        var categoryName = "Electronics";
        var categorySlug = "electronics";
        var variants = new List<ProductVariantDto>();
        var images = new List<ProductImageDto>();

        // Act
        var dto = ProductMapper.ToDto(product, categoryName, categorySlug, variants, images);

        // Assert
        dto.Id.ShouldBe(product.Id);
        dto.Name.ShouldBe("Test Product");
        dto.Slug.ShouldBe("test-product");
        dto.Description.ShouldBe("Description");
        dto.DescriptionHtml.ShouldBe("<p>HTML</p>");
        dto.BasePrice.ShouldBe(99.99m);
        dto.Currency.ShouldBe("VND");
        dto.CategoryName.ShouldBe(categoryName);
        dto.CategorySlug.ShouldBe(categorySlug);
        dto.Brand.ShouldBe("TestBrand");
        dto.Sku.ShouldBe("SKU-001");
        dto.Barcode.ShouldBe("BARCODE-001");
        dto.TrackInventory.ShouldBe(true);
        dto.MetaTitle.ShouldBe("Meta Title");
        dto.MetaDescription.ShouldBe("Meta Description");
        dto.Variants.ShouldBeSameAs(variants);
        dto.Images.ShouldBeSameAs(images);
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
        dto.CategoryId.ShouldBeNull();
        dto.CategoryName.ShouldBeNull();
        dto.CategorySlug.ShouldBeNull();
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
        dto.Id.ShouldBe(product.Id);
        dto.Variants.Count().ShouldBe(1);
        dto.Variants[0].Name.ShouldBe("Default");
        dto.Images.Count().ShouldBe(1);
        dto.Images[0].Url.ShouldBe("https://example.com/img.jpg");
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
        dto.Variants.Count().ShouldBe(3);
        dto.Variants[0].Name.ShouldBe("First");
        dto.Variants[1].Name.ShouldBe("Second");
        dto.Variants[2].Name.ShouldBe("Third");
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
        dto.Images.Count().ShouldBe(3);
        dto.Images[0].AltText.ShouldBe("First");
        dto.Images[1].AltText.ShouldBe("Second");
        dto.Images[2].AltText.ShouldBe("Third");
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
        dto.CategoryName.ShouldBe("Category");
        dto.CategorySlug.ShouldBe("category-slug");
        dto.Variants.Count().ShouldBe(2);
        dto.Images.Count().ShouldBe(1);
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
        dto.Id.ShouldBe(product.Id);
        dto.Name.ShouldBe("List Product");
        dto.Slug.ShouldBe("list-product");
        dto.BasePrice.ShouldBe(199.99m);
        dto.Currency.ShouldBe("USD");
        dto.Brand.ShouldBe("ListBrand");
        dto.Sku.ShouldBe("LIST-SKU");
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
        dto.PrimaryImageUrl.ShouldBe("https://example.com/primary.jpg");
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
        dto.PrimaryImageUrl.ShouldBe("https://example.com/first.jpg");
    }

    [Fact]
    public void ToListDto_ReturnsNullImageUrl_WhenNoImages()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        var dto = ProductMapper.ToListDto(product);

        // Assert
        dto.PrimaryImageUrl.ShouldBeNull();
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
        dto.Id.ShouldBe(variant.Id);
        dto.Name.ShouldBe("Large Red");
        dto.Sku.ShouldBe("VAR-001");
        dto.Price.ShouldBe(149.99m);
        dto.CompareAtPrice.ShouldBe(199.99m);
        dto.StockQuantity.ShouldBe(50);
        dto.InStock.ShouldBe(true);
        dto.OnSale.ShouldBe(true);
        dto.SortOrder.ShouldBe(2);
        dto.Options.ShouldContainKey("Size");
        dto.Options["Size"].ShouldBe("Large");
        dto.Options.ShouldContainKey("Color");
        dto.Options["Color"].ShouldBe("Red");
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
        dto.InStock.ShouldBe(true);
        dto.LowStock.ShouldBe(true);
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
        dto.InStock.ShouldBe(false);
        dto.StockQuantity.ShouldBe(0);
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
        dto.Id.ShouldBe(image.Id);
        dto.Url.ShouldBe("https://cdn.example.com/product/image.jpg");
        dto.AltText.ShouldBe("Product image");
        dto.IsPrimary.ShouldBe(true);
        dto.SortOrder.ShouldBe(1);
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
        dto.AltText.ShouldBeNull();
        dto.IsPrimary.ShouldBe(false);
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
        dto.Id.ShouldBe(category.Id);
        dto.Name.ShouldBe("Child Category");
        dto.Slug.ShouldBe("child-category");
        dto.Description.ShouldBe("A child category");
        dto.ImageUrl.ShouldBe("https://example.com/img.jpg");
        dto.MetaTitle.ShouldBe("Child Meta");
        dto.MetaDescription.ShouldBe("Child Meta Description");
        dto.SortOrder.ShouldBe(5);
        dto.ParentId.ShouldBe(parentId);
        dto.ParentName.ShouldBe("Parent Category");
        dto.Children.ShouldBeNull(); // Children not loaded in command context
    }

    [Fact]
    public void ToDto_Category_WithNullParent()
    {
        // Arrange
        var category = CreateTestCategory("Root Category", "root-category");

        // Act
        var dto = ProductMapper.ToDto(category, null);

        // Assert
        dto.ParentId.ShouldBeNull();
        dto.ParentName.ShouldBeNull();
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
        dto.Children.Count().ShouldBe(2);
        dto.Children.ShouldContain(c => c.Name == "Child 1");
        dto.Children.ShouldContain(c => c.Name == "Child 2");
    }

    [Fact]
    public void ToDtoWithChildren_HandlesNullChildren()
    {
        // Arrange
        var category = CreateTestCategory();

        // Act
        var dto = ProductMapper.ToDtoWithChildren(category, null);

        // Assert
        dto.Children.ShouldBeNull();
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
        dto.Id.ShouldBe(category.Id);
        dto.Name.ShouldBe("List Category");
        dto.Slug.ShouldBe("list-category");
        dto.Description.ShouldBe("Description");
        dto.SortOrder.ShouldBe(10);
        dto.ProductCount.ShouldBe(0);
        dto.ParentId.ShouldBeNull();
        dto.ParentName.ShouldBeNull();
        dto.ChildCount.ShouldBe(0);
    }

    #endregion
}
