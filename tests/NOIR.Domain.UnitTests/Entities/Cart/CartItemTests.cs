using NOIR.Domain.Entities.Cart;

namespace NOIR.Domain.UnitTests.Entities.Cart;

/// <summary>
/// Unit tests for the CartItem entity.
/// Tests factory methods, quantity updates, price updates, snapshot updates,
/// and line total computation.
/// </summary>
public class CartItemTests
{
    private const string TestTenantId = "test-tenant";

    private static readonly Guid TestProductId = Guid.NewGuid();
    private static readonly Guid TestVariantId = Guid.NewGuid();

    #region Helper Methods

    private static CartItem CreateTestCartItem(
        int quantity = 2,
        decimal unitPrice = 50000m,
        string? imageUrl = "http://img.jpg")
    {
        // CartItem.Create is internal, so we use Cart.AddItem to get a CartItem
        var cart = Domain.Entities.Cart.Cart.CreateForUser("user-123", "VND", TestTenantId);
        return cart.AddItem(TestProductId, TestVariantId, "Test Product", "Size: M", unitPrice, quantity, imageUrl);
    }

    #endregion

    #region Creation Tests (via Cart.AddItem)

    [Fact]
    public void Create_WithValidParameters_ShouldSetAllProperties()
    {
        // Act
        var item = CreateTestCartItem(quantity: 3, unitPrice: 25000m);

        // Assert
        item.ShouldNotBeNull();
        item.Id.ShouldNotBe(Guid.Empty);
        item.ProductId.ShouldBe(TestProductId);
        item.ProductVariantId.ShouldBe(TestVariantId);
        item.ProductName.ShouldBe("Test Product");
        item.VariantName.ShouldBe("Size: M");
        item.UnitPrice.ShouldBe(25000m);
        item.Quantity.ShouldBe(3);
        item.ImageUrl.ShouldBe("http://img.jpg");
    }

    [Fact]
    public void Create_WithNullImageUrl_ShouldAllowNull()
    {
        // Act
        var item = CreateTestCartItem(imageUrl: null);

        // Assert
        item.ImageUrl.ShouldBeNull();
    }

    [Fact]
    public void Create_WithZeroQuantity_ShouldThrowViaCart()
    {
        // Arrange
        var cart = Domain.Entities.Cart.Cart.CreateForUser("user-123", "VND", TestTenantId);

        // Act
        var act = () => cart.AddItem(TestProductId, TestVariantId, "Product", "Variant", 10000m, 0);

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Quantity must be greater than zero");
    }

    #endregion

    #region LineTotal Tests

    [Fact]
    public void LineTotal_ShouldCalculateUnitPriceTimesQuantity()
    {
        // Arrange
        var item = CreateTestCartItem(quantity: 3, unitPrice: 25000m);

        // Act & Assert
        item.LineTotal.ShouldBe(75000m); // 25,000 * 3
    }

    [Theory]
    [InlineData(1, 10000, 10000)]
    [InlineData(5, 20000, 100000)]
    [InlineData(10, 1500, 15000)]
    [InlineData(1, 0, 0)]
    public void LineTotal_VariousQuantitiesAndPrices_ShouldCalculateCorrectly(
        int quantity, decimal unitPrice, decimal expectedTotal)
    {
        // Arrange
        var item = CreateTestCartItem(quantity: quantity, unitPrice: unitPrice);

        // Act & Assert
        item.LineTotal.ShouldBe(expectedTotal);
    }

    #endregion

    #region UpdateQuantity Tests

    [Fact]
    public void UpdateQuantity_WithValidQuantity_ShouldUpdateSuccessfully()
    {
        // Arrange
        var item = CreateTestCartItem(quantity: 2);

        // Act
        item.UpdateQuantity(10);

        // Assert
        item.Quantity.ShouldBe(10);
    }

    [Fact]
    public void UpdateQuantity_ShouldUpdateLineTotal()
    {
        // Arrange
        var item = CreateTestCartItem(quantity: 2, unitPrice: 10000m);
        item.LineTotal.ShouldBe(20000m);

        // Act
        item.UpdateQuantity(5);

        // Assert
        item.LineTotal.ShouldBe(50000m);
    }

    [Fact]
    public void UpdateQuantity_WithZero_ShouldThrow()
    {
        // Arrange
        var item = CreateTestCartItem();

        // Act
        var act = () => item.UpdateQuantity(0);

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Quantity must be greater than zero");
    }

    [Fact]
    public void UpdateQuantity_WithNegative_ShouldThrow()
    {
        // Arrange
        var item = CreateTestCartItem();

        // Act
        var act = () => item.UpdateQuantity(-1);

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Quantity must be greater than zero");
    }

    #endregion

    #region UpdatePrice Tests

    [Fact]
    public void UpdatePrice_WithValidPrice_ShouldUpdateSuccessfully()
    {
        // Arrange
        var item = CreateTestCartItem(unitPrice: 10000m);

        // Act
        item.UpdatePrice(15000m);

        // Assert
        item.UnitPrice.ShouldBe(15000m);
    }

    [Fact]
    public void UpdatePrice_ShouldUpdateLineTotal()
    {
        // Arrange
        var item = CreateTestCartItem(quantity: 3, unitPrice: 10000m);

        // Act
        item.UpdatePrice(20000m);

        // Assert
        item.LineTotal.ShouldBe(60000m); // 20,000 * 3
    }

    [Fact]
    public void UpdatePrice_WithZero_ShouldSucceed()
    {
        // Arrange
        var item = CreateTestCartItem(unitPrice: 10000m);

        // Act
        item.UpdatePrice(0m);

        // Assert
        item.UnitPrice.ShouldBe(0m);
    }

    [Fact]
    public void UpdatePrice_WithNegative_ShouldThrow()
    {
        // Arrange
        var item = CreateTestCartItem();

        // Act
        var act = () => item.UpdatePrice(-1m);

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Unit price cannot be negative");
    }

    #endregion

    #region UpdateProductSnapshot Tests

    [Fact]
    public void UpdateProductSnapshot_ShouldUpdateAllSnapshotFields()
    {
        // Arrange
        var item = CreateTestCartItem();

        // Act
        item.UpdateProductSnapshot("New Product Name", "New Variant", "http://new-img.jpg", 99000m);

        // Assert
        item.ProductName.ShouldBe("New Product Name");
        item.VariantName.ShouldBe("New Variant");
        item.ImageUrl.ShouldBe("http://new-img.jpg");
        item.UnitPrice.ShouldBe(99000m);
    }

    [Fact]
    public void UpdateProductSnapshot_WithNullImageUrl_ShouldSetNull()
    {
        // Arrange
        var item = CreateTestCartItem(imageUrl: "http://old.jpg");

        // Act
        item.UpdateProductSnapshot("Name", "Variant", null, 10000m);

        // Assert
        item.ImageUrl.ShouldBeNull();
    }

    [Fact]
    public void UpdateProductSnapshot_ShouldAffectLineTotal()
    {
        // Arrange
        var item = CreateTestCartItem(quantity: 4, unitPrice: 10000m);

        // Act
        item.UpdateProductSnapshot("Name", "Variant", null, 25000m);

        // Assert
        item.LineTotal.ShouldBe(100000m); // 25,000 * 4
    }

    #endregion
}
