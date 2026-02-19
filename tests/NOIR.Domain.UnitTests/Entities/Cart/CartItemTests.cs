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
        item.Should().NotBeNull();
        item.Id.Should().NotBe(Guid.Empty);
        item.ProductId.Should().Be(TestProductId);
        item.ProductVariantId.Should().Be(TestVariantId);
        item.ProductName.Should().Be("Test Product");
        item.VariantName.Should().Be("Size: M");
        item.UnitPrice.Should().Be(25000m);
        item.Quantity.Should().Be(3);
        item.ImageUrl.Should().Be("http://img.jpg");
    }

    [Fact]
    public void Create_WithNullImageUrl_ShouldAllowNull()
    {
        // Act
        var item = CreateTestCartItem(imageUrl: null);

        // Assert
        item.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void Create_WithZeroQuantity_ShouldThrowViaCart()
    {
        // Arrange
        var cart = Domain.Entities.Cart.Cart.CreateForUser("user-123", "VND", TestTenantId);

        // Act
        var act = () => cart.AddItem(TestProductId, TestVariantId, "Product", "Variant", 10000m, 0);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Quantity must be greater than zero");
    }

    #endregion

    #region LineTotal Tests

    [Fact]
    public void LineTotal_ShouldCalculateUnitPriceTimesQuantity()
    {
        // Arrange
        var item = CreateTestCartItem(quantity: 3, unitPrice: 25000m);

        // Act & Assert
        item.LineTotal.Should().Be(75000m); // 25,000 * 3
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
        item.LineTotal.Should().Be(expectedTotal);
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
        item.Quantity.Should().Be(10);
    }

    [Fact]
    public void UpdateQuantity_ShouldUpdateLineTotal()
    {
        // Arrange
        var item = CreateTestCartItem(quantity: 2, unitPrice: 10000m);
        item.LineTotal.Should().Be(20000m);

        // Act
        item.UpdateQuantity(5);

        // Assert
        item.LineTotal.Should().Be(50000m);
    }

    [Fact]
    public void UpdateQuantity_WithZero_ShouldThrow()
    {
        // Arrange
        var item = CreateTestCartItem();

        // Act
        var act = () => item.UpdateQuantity(0);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Quantity must be greater than zero");
    }

    [Fact]
    public void UpdateQuantity_WithNegative_ShouldThrow()
    {
        // Arrange
        var item = CreateTestCartItem();

        // Act
        var act = () => item.UpdateQuantity(-1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Quantity must be greater than zero");
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
        item.UnitPrice.Should().Be(15000m);
    }

    [Fact]
    public void UpdatePrice_ShouldUpdateLineTotal()
    {
        // Arrange
        var item = CreateTestCartItem(quantity: 3, unitPrice: 10000m);

        // Act
        item.UpdatePrice(20000m);

        // Assert
        item.LineTotal.Should().Be(60000m); // 20,000 * 3
    }

    [Fact]
    public void UpdatePrice_WithZero_ShouldSucceed()
    {
        // Arrange
        var item = CreateTestCartItem(unitPrice: 10000m);

        // Act
        item.UpdatePrice(0m);

        // Assert
        item.UnitPrice.Should().Be(0m);
    }

    [Fact]
    public void UpdatePrice_WithNegative_ShouldThrow()
    {
        // Arrange
        var item = CreateTestCartItem();

        // Act
        var act = () => item.UpdatePrice(-1m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Unit price cannot be negative");
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
        item.ProductName.Should().Be("New Product Name");
        item.VariantName.Should().Be("New Variant");
        item.ImageUrl.Should().Be("http://new-img.jpg");
        item.UnitPrice.Should().Be(99000m);
    }

    [Fact]
    public void UpdateProductSnapshot_WithNullImageUrl_ShouldSetNull()
    {
        // Arrange
        var item = CreateTestCartItem(imageUrl: "http://old.jpg");

        // Act
        item.UpdateProductSnapshot("Name", "Variant", null, 10000m);

        // Assert
        item.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void UpdateProductSnapshot_ShouldAffectLineTotal()
    {
        // Arrange
        var item = CreateTestCartItem(quantity: 4, unitPrice: 10000m);

        // Act
        item.UpdateProductSnapshot("Name", "Variant", null, 25000m);

        // Assert
        item.LineTotal.Should().Be(100000m); // 25,000 * 4
    }

    #endregion
}
