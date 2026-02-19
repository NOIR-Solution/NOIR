using NOIR.Domain.Entities.Product;

namespace NOIR.Domain.UnitTests.Entities.Product.ProductImage;

/// <summary>
/// Unit tests for the ProductImage entity.
/// Tests creation via parent Product, update methods, sort order,
/// primary flag management, and property validation.
/// ProductImage.Create is internal, so instances are created via Product.AddImage.
/// </summary>
public class ProductImageTests
{
    private const string TestTenantId = "test-tenant";

    #region Helper Methods

    private static Domain.Entities.Product.Product CreateTestProduct()
    {
        return Domain.Entities.Product.Product.Create("Test Product", "test-product", 100_000m, "VND", TestTenantId);
    }

    private static Domain.Entities.Product.ProductImage CreateTestImage(
        string url = "https://example.com/img.jpg",
        string? altText = "Product image",
        bool isPrimary = false)
    {
        var product = CreateTestProduct();
        return product.AddImage(url, altText, isPrimary);
    }

    #endregion

    #region Creation Tests (via Product.AddImage)

    [Fact]
    public void Create_ViaProduct_ShouldSetAllProperties()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        var image = product.AddImage("https://example.com/photo.jpg", "Alt text", true);

        // Assert
        image.Should().NotBeNull();
        image.Id.Should().NotBe(Guid.Empty);
        image.ProductId.Should().Be(product.Id);
        image.Url.Should().Be("https://example.com/photo.jpg");
        image.AltText.Should().Be("Alt text");
        image.IsPrimary.Should().BeTrue();
        image.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_FirstImage_ShouldHaveSortOrderZero()
    {
        // Act
        var image = CreateTestImage();

        // Assert
        image.SortOrder.Should().Be(0);
    }

    [Fact]
    public void Create_SecondImage_ShouldIncrementSortOrder()
    {
        // Arrange
        var product = CreateTestProduct();
        product.AddImage("https://example.com/1.jpg");

        // Act
        var second = product.AddImage("https://example.com/2.jpg");

        // Assert
        second.SortOrder.Should().Be(1);
    }

    [Fact]
    public void Create_WithNullAltText_ShouldAllowNull()
    {
        // Act
        var image = CreateTestImage(altText: null);

        // Assert
        image.AltText.Should().BeNull();
    }

    [Fact]
    public void Create_WithIsPrimaryTrue_ShouldClearOtherPrimaries()
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
    public void Create_WithIsPrimaryFalse_ShouldNotAffectExistingPrimary()
    {
        // Arrange
        var product = CreateTestProduct();
        var first = product.AddImage("https://example.com/1.jpg", isPrimary: true);

        // Act
        var second = product.AddImage("https://example.com/2.jpg", isPrimary: false);

        // Assert
        first.IsPrimary.Should().BeTrue();
        second.IsPrimary.Should().BeFalse();
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldChangeUrlAndAltText()
    {
        // Arrange
        var image = CreateTestImage(url: "https://old.com/img.jpg", altText: "Old alt");

        // Act
        image.Update("https://new.com/img.jpg", "New alt");

        // Assert
        image.Url.Should().Be("https://new.com/img.jpg");
        image.AltText.Should().Be("New alt");
    }

    [Fact]
    public void Update_WithNullAltText_ShouldSetNull()
    {
        // Arrange
        var image = CreateTestImage(altText: "Has alt");

        // Act
        image.Update("https://new.com/img.jpg", null);

        // Assert
        image.AltText.Should().BeNull();
    }

    #endregion

    #region SetSortOrder Tests

    [Fact]
    public void SetSortOrder_ShouldUpdateValue()
    {
        // Arrange
        var image = CreateTestImage();

        // Act
        image.SetSortOrder(5);

        // Assert
        image.SortOrder.Should().Be(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void SetSortOrder_VariousValues_ShouldSetCorrectly(int sortOrder)
    {
        // Arrange
        var image = CreateTestImage();

        // Act
        image.SetSortOrder(sortOrder);

        // Assert
        image.SortOrder.Should().Be(sortOrder);
    }

    #endregion

    #region SetAsPrimary Tests

    [Fact]
    public void SetAsPrimary_ShouldSetIsPrimaryTrue()
    {
        // Arrange
        var image = CreateTestImage(isPrimary: false);

        // Act
        image.SetAsPrimary();

        // Assert
        image.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void SetAsPrimary_WhenAlreadyPrimary_ShouldRemainPrimary()
    {
        // Arrange
        var image = CreateTestImage(isPrimary: true);

        // Act
        image.SetAsPrimary();

        // Assert
        image.IsPrimary.Should().BeTrue();
    }

    #endregion

    #region ClearPrimary Tests

    [Fact]
    public void ClearPrimary_ShouldSetIsPrimaryFalse()
    {
        // Arrange
        var image = CreateTestImage(isPrimary: true);

        // Act
        image.ClearPrimary();

        // Assert
        image.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void ClearPrimary_WhenNotPrimary_ShouldRemainFalse()
    {
        // Arrange
        var image = CreateTestImage(isPrimary: false);

        // Act
        image.ClearPrimary();

        // Assert
        image.IsPrimary.Should().BeFalse();
    }

    #endregion
}
