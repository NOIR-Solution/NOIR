using NOIR.Domain.Entities.Order;

namespace NOIR.Domain.UnitTests.Entities.Order;

/// <summary>
/// Unit tests for the OrderItem entity.
/// Tests factory method, computed properties (Subtotal, LineTotal),
/// discount/tax mutations, and edge cases.
/// </summary>
public class OrderItemTests
{
    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestOrderId = Guid.NewGuid();
    private static readonly Guid TestProductId = Guid.NewGuid();
    private static readonly Guid TestVariantId = Guid.NewGuid();

    #region Helper Methods

    /// <summary>
    /// Creates an OrderItem via the parent Order.AddItem to respect internal factory.
    /// </summary>
    private static OrderItem CreateTestOrderItem(
        decimal unitPrice = 100_000m,
        int quantity = 2,
        string? sku = "SKU-001",
        string? imageUrl = "https://img.example.com/product.jpg",
        string? optionsSnapshot = "Color: Red, Size: M",
        string? tenantId = TestTenantId)
    {
        var order = NOIR.Domain.Entities.Order.Order.Create(
            "ORD-TEST-0001", "test@example.com", 0m, 0m, "VND", tenantId);
        return order.AddItem(
            TestProductId, TestVariantId,
            "Test Product", "Size M - Red",
            unitPrice, quantity,
            sku, imageUrl, optionsSnapshot);
    }

    /// <summary>
    /// Creates an OrderItem via the static factory (public API).
    /// </summary>
    private static OrderItem CreateViaStaticFactory(
        Guid? orderId = null,
        decimal unitPrice = 100_000m,
        int quantity = 2,
        string? sku = null,
        string? imageUrl = null,
        string? optionsSnapshot = null,
        string? tenantId = TestTenantId)
    {
        return OrderItem.Create(
            orderId ?? TestOrderId,
            TestProductId,
            TestVariantId,
            "Test Product",
            "Size M",
            unitPrice,
            quantity,
            sku,
            imageUrl,
            optionsSnapshot,
            tenantId);
    }

    #endregion

    #region Create Factory Tests

    [Fact]
    public void Create_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        // Act
        var item = OrderItem.Create(
            orderId, productId, variantId,
            "Ao Thun Nam", "Size M - Blue",
            250_000m, 3,
            "SKU-AT-001", "https://img.example.com/ao-thun.jpg",
            "Color: Blue, Size: M",
            TestTenantId);

        // Assert
        item.Should().NotBeNull();
        item.Id.Should().NotBe(Guid.Empty);
        item.OrderId.Should().Be(orderId);
        item.ProductId.Should().Be(productId);
        item.ProductVariantId.Should().Be(variantId);
        item.ProductName.Should().Be("Ao Thun Nam");
        item.VariantName.Should().Be("Size M - Blue");
        item.UnitPrice.Should().Be(250_000m);
        item.Quantity.Should().Be(3);
        item.Sku.Should().Be("SKU-AT-001");
        item.ImageUrl.Should().Be("https://img.example.com/ao-thun.jpg");
        item.OptionsSnapshot.Should().Be("Color: Blue, Size: M");
        item.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldInitializeDiscountAndTaxToZero()
    {
        // Act
        var item = CreateViaStaticFactory();

        // Assert
        item.DiscountAmount.Should().Be(0);
        item.TaxAmount.Should().Be(0);
    }

    [Fact]
    public void Create_WithNullOptionalParameters_ShouldAllowNulls()
    {
        // Act
        var item = OrderItem.Create(
            TestOrderId, TestProductId, TestVariantId,
            "Product", "Variant", 50_000m, 1);

        // Assert
        item.Sku.Should().BeNull();
        item.ImageUrl.Should().BeNull();
        item.OptionsSnapshot.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var item = CreateViaStaticFactory(tenantId: null);

        // Assert
        item.TenantId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Act
        var item1 = CreateViaStaticFactory();
        var item2 = CreateViaStaticFactory();

        // Assert
        item1.Id.Should().NotBe(item2.Id);
    }

    #endregion

    #region Subtotal Tests

    [Fact]
    public void Subtotal_ShouldBeUnitPriceTimesQuantity()
    {
        // Arrange
        var item = CreateViaStaticFactory(unitPrice: 150_000m, quantity: 4);

        // Assert
        item.Subtotal.Should().Be(600_000m);
    }

    [Theory]
    [InlineData(100_000, 1, 100_000)]
    [InlineData(50_000, 5, 250_000)]
    [InlineData(10_000, 100, 1_000_000)]
    [InlineData(1, 1, 1)]
    public void Subtotal_VariousValues_ShouldCalculateCorrectly(
        decimal unitPrice, int quantity, decimal expectedSubtotal)
    {
        // Arrange
        var item = CreateViaStaticFactory(unitPrice: unitPrice, quantity: quantity);

        // Assert
        item.Subtotal.Should().Be(expectedSubtotal);
    }

    #endregion

    #region LineTotal Tests

    [Fact]
    public void LineTotal_WithNoDiscountOrTax_ShouldEqualSubtotal()
    {
        // Arrange
        var item = CreateViaStaticFactory(unitPrice: 200_000m, quantity: 2);

        // Assert
        item.LineTotal.Should().Be(item.Subtotal);
        item.LineTotal.Should().Be(400_000m);
    }

    [Fact]
    public void LineTotal_WithDiscount_ShouldSubtractDiscount()
    {
        // Arrange
        var item = CreateViaStaticFactory(unitPrice: 100_000m, quantity: 3);
        item.SetDiscount(20_000m);

        // Assert - LineTotal = (100000 * 3) - 20000 + 0 = 280000
        item.LineTotal.Should().Be(280_000m);
    }

    [Fact]
    public void LineTotal_WithTax_ShouldAddTax()
    {
        // Arrange
        var item = CreateViaStaticFactory(unitPrice: 100_000m, quantity: 2);
        item.SetTax(15_000m);

        // Assert - LineTotal = (100000 * 2) - 0 + 15000 = 215000
        item.LineTotal.Should().Be(215_000m);
    }

    [Fact]
    public void LineTotal_WithDiscountAndTax_ShouldCalculateCorrectly()
    {
        // Arrange
        var item = CreateViaStaticFactory(unitPrice: 100_000m, quantity: 3);
        item.SetDiscount(20_000m);
        item.SetTax(15_000m);

        // Assert - LineTotal = (100000 * 3) - 20000 + 15000 = 295000
        item.LineTotal.Should().Be(295_000m);
    }

    [Fact]
    public void LineTotal_ShouldFollowFormula_SubtotalMinusDiscountPlusTax()
    {
        // Arrange
        var item = CreateViaStaticFactory(unitPrice: 500_000m, quantity: 2);
        item.SetDiscount(100_000m);
        item.SetTax(50_000m);

        // Assert - LineTotal = (500000 * 2) - 100000 + 50000 = 950000
        var expected = item.Subtotal - item.DiscountAmount + item.TaxAmount;
        item.LineTotal.Should().Be(expected);
        item.LineTotal.Should().Be(950_000m);
    }

    #endregion

    #region SetDiscount Tests

    [Fact]
    public void SetDiscount_WithValidAmount_ShouldSetDiscount()
    {
        // Arrange
        var item = CreateViaStaticFactory();

        // Act
        item.SetDiscount(50_000m);

        // Assert
        item.DiscountAmount.Should().Be(50_000m);
    }

    [Fact]
    public void SetDiscount_WithZero_ShouldSucceed()
    {
        // Arrange
        var item = CreateViaStaticFactory();
        item.SetDiscount(50_000m);

        // Act
        item.SetDiscount(0m);

        // Assert
        item.DiscountAmount.Should().Be(0);
    }

    [Fact]
    public void SetDiscount_ShouldOverwritePreviousValue()
    {
        // Arrange
        var item = CreateViaStaticFactory();
        item.SetDiscount(30_000m);

        // Act
        item.SetDiscount(50_000m);

        // Assert
        item.DiscountAmount.Should().Be(50_000m);
    }

    [Fact]
    public void SetDiscount_WithNegativeAmount_ShouldThrow()
    {
        // Arrange
        var item = CreateViaStaticFactory();

        // Act
        var act = () => item.SetDiscount(-10_000m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Discount amount cannot be negative");
    }

    [Fact]
    public void SetDiscount_ShouldAffectLineTotal()
    {
        // Arrange
        var item = CreateViaStaticFactory(unitPrice: 100_000m, quantity: 2);
        item.LineTotal.Should().Be(200_000m);

        // Act
        item.SetDiscount(30_000m);

        // Assert
        item.LineTotal.Should().Be(170_000m);
    }

    #endregion

    #region SetTax Tests

    [Fact]
    public void SetTax_WithValidAmount_ShouldSetTax()
    {
        // Arrange
        var item = CreateViaStaticFactory();

        // Act
        item.SetTax(10_000m);

        // Assert
        item.TaxAmount.Should().Be(10_000m);
    }

    [Fact]
    public void SetTax_WithZero_ShouldSucceed()
    {
        // Arrange
        var item = CreateViaStaticFactory();
        item.SetTax(10_000m);

        // Act
        item.SetTax(0m);

        // Assert
        item.TaxAmount.Should().Be(0);
    }

    [Fact]
    public void SetTax_ShouldOverwritePreviousValue()
    {
        // Arrange
        var item = CreateViaStaticFactory();
        item.SetTax(10_000m);

        // Act
        item.SetTax(25_000m);

        // Assert
        item.TaxAmount.Should().Be(25_000m);
    }

    [Fact]
    public void SetTax_WithNegativeAmount_ShouldThrow()
    {
        // Arrange
        var item = CreateViaStaticFactory();

        // Act
        var act = () => item.SetTax(-5_000m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Tax amount cannot be negative");
    }

    [Fact]
    public void SetTax_ShouldAffectLineTotal()
    {
        // Arrange
        var item = CreateViaStaticFactory(unitPrice: 100_000m, quantity: 2);
        item.LineTotal.Should().Be(200_000m);

        // Act
        item.SetTax(20_000m);

        // Assert
        item.LineTotal.Should().Be(220_000m);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void OrderItem_WithLargeQuantity_ShouldCalculateCorrectly()
    {
        // Arrange
        var item = CreateViaStaticFactory(unitPrice: 10_000m, quantity: 10_000);

        // Assert
        item.Subtotal.Should().Be(100_000_000m);
        item.LineTotal.Should().Be(100_000_000m);
    }

    [Fact]
    public void OrderItem_WithSingleQuantity_ShouldEqualUnitPrice()
    {
        // Arrange
        var item = CreateViaStaticFactory(unitPrice: 99_000m, quantity: 1);

        // Assert
        item.Subtotal.Should().Be(99_000m);
    }

    [Fact]
    public void OrderItem_DiscountExceedingSubtotal_ShouldAllowNegativeLineTotal()
    {
        // Arrange - business logic should prevent this at application layer
        var item = CreateViaStaticFactory(unitPrice: 100_000m, quantity: 1);

        // Act
        item.SetDiscount(150_000m);

        // Assert - entity doesn't enforce discount <= subtotal
        item.LineTotal.Should().Be(-50_000m);
    }

    [Fact]
    public void OrderItem_SetDiscountThenTax_ShouldComputeCorrectLineTotal()
    {
        // Arrange
        var item = CreateViaStaticFactory(unitPrice: 200_000m, quantity: 3);

        // Act
        item.SetDiscount(50_000m);
        item.SetTax(30_000m);

        // Assert - LineTotal = (200000 * 3) - 50000 + 30000 = 580000
        item.LineTotal.Should().Be(580_000m);
    }

    #endregion
}
