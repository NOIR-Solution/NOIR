using NOIR.Domain.Entities.Inventory;

namespace NOIR.Domain.UnitTests.Entities.Inventory;

/// <summary>
/// Unit tests for the InventoryReceiptItem entity.
/// InventoryReceiptItem.Create is internal, so all items are created via InventoryReceipt.AddItem.
/// Tests property initialization, computed LineTotal, and various quantity/cost combinations.
/// </summary>
public class InventoryReceiptItemTests
{
    private const string TestTenantId = "test-tenant";

    #region Helper Methods

    private static InventoryReceipt CreateDraftReceipt(string? tenantId = TestTenantId)
    {
        return InventoryReceipt.Create("RCV-TEST-0001", InventoryReceiptType.StockIn, tenantId: tenantId);
    }

    private static InventoryReceiptItem AddTestItem(
        InventoryReceipt receipt,
        int quantity = 10,
        decimal unitCost = 25_000m,
        string productName = "Test Product",
        string variantName = "Size: M",
        string? sku = "SKU-001")
    {
        return receipt.AddItem(
            Guid.NewGuid(), Guid.NewGuid(),
            productName, variantName, sku,
            quantity, unitCost);
    }

    #endregion

    #region Creation Tests (via InventoryReceipt.AddItem)

    [Fact]
    public void Create_ViaReceipt_ShouldSetAllProperties()
    {
        // Arrange
        var receipt = CreateDraftReceipt();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // Act
        var item = receipt.AddItem(variantId, productId, "Ao Polo", "Size L", "SKU-AP-L", 20, 150_000m);

        // Assert
        item.ShouldNotBeNull();
        item.Id.ShouldNotBe(Guid.Empty);
        item.InventoryReceiptId.ShouldBe(receipt.Id);
        item.ProductVariantId.ShouldBe(variantId);
        item.ProductId.ShouldBe(productId);
        item.ProductName.ShouldBe("Ao Polo");
        item.VariantName.ShouldBe("Size L");
        item.Sku.ShouldBe("SKU-AP-L");
        item.Quantity.ShouldBe(20);
        item.UnitCost.ShouldBe(150_000m);
    }

    [Fact]
    public void Create_ShouldSetTenantIdFromReceipt()
    {
        // Arrange
        var receipt = CreateDraftReceipt(tenantId: "custom-tenant");

        // Act
        var item = AddTestItem(receipt);

        // Assert
        item.TenantId.ShouldBe("custom-tenant");
    }

    [Fact]
    public void Create_WithNullSku_ShouldAllowNull()
    {
        // Arrange
        var receipt = CreateDraftReceipt();

        // Act
        var item = receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", null, 5, 10_000m);

        // Assert
        item.Sku.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Arrange
        var receipt = CreateDraftReceipt();

        // Act
        var item1 = AddTestItem(receipt);
        var item2 = AddTestItem(receipt);

        // Assert
        item1.Id.ShouldNotBe(item2.Id);
    }

    #endregion

    #region LineTotal Computed Property Tests

    [Fact]
    public void LineTotal_ShouldBeQuantityTimesUnitCost()
    {
        // Arrange
        var receipt = CreateDraftReceipt();
        var item = AddTestItem(receipt, quantity: 10, unitCost: 25_000m);

        // Assert
        item.LineTotal.ShouldBe(250_000m);
    }

    [Theory]
    [InlineData(1, 10_000, 10_000)]
    [InlineData(5, 20_000, 100_000)]
    [InlineData(100, 1_500, 150_000)]
    [InlineData(1000, 50, 50_000)]
    public void LineTotal_VariousCombinations_ShouldCalculateCorrectly(
        int quantity, decimal unitCost, decimal expectedTotal)
    {
        // Arrange
        var receipt = CreateDraftReceipt();
        var item = AddTestItem(receipt, quantity: quantity, unitCost: unitCost);

        // Assert
        item.LineTotal.ShouldBe(expectedTotal);
    }

    [Fact]
    public void LineTotal_WithZeroUnitCost_ShouldBeZero()
    {
        // Arrange
        var receipt = CreateDraftReceipt();
        var item = AddTestItem(receipt, quantity: 100, unitCost: 0m);

        // Assert
        item.LineTotal.ShouldBe(0m);
    }

    #endregion

    #region Integration with Receipt Computed Properties

    [Fact]
    public void Receipt_TotalQuantity_ShouldSumAllItemQuantities()
    {
        // Arrange
        var receipt = CreateDraftReceipt();
        AddTestItem(receipt, quantity: 10);
        AddTestItem(receipt, quantity: 20);
        AddTestItem(receipt, quantity: 30);

        // Assert
        receipt.TotalQuantity.ShouldBe(60);
    }

    [Fact]
    public void Receipt_TotalCost_ShouldSumAllItemLineTotals()
    {
        // Arrange
        var receipt = CreateDraftReceipt();
        AddTestItem(receipt, quantity: 10, unitCost: 10_000m);  // 100,000
        AddTestItem(receipt, quantity: 5, unitCost: 20_000m);   // 100,000
        AddTestItem(receipt, quantity: 3, unitCost: 50_000m);   // 150,000

        // Assert
        receipt.TotalCost.ShouldBe(350_000m);
    }

    [Fact]
    public void Receipt_WithNoItems_ShouldHaveZeroTotals()
    {
        // Arrange
        var receipt = CreateDraftReceipt();

        // Assert
        receipt.TotalQuantity.ShouldBe(0);
        receipt.TotalCost.ShouldBe(0m);
    }

    #endregion

    #region Snapshot Properties

    [Fact]
    public void Create_ShouldCaptureProductNameSnapshot()
    {
        // Arrange
        var receipt = CreateDraftReceipt();

        // Act
        var item = AddTestItem(receipt, productName: "Original Product Name");

        // Assert
        item.ProductName.ShouldBe("Original Product Name");
    }

    [Fact]
    public void Create_ShouldCaptureVariantNameSnapshot()
    {
        // Arrange
        var receipt = CreateDraftReceipt();

        // Act
        var item = AddTestItem(receipt, variantName: "Color: Red, Size: XL");

        // Assert
        item.VariantName.ShouldBe("Color: Red, Size: XL");
    }

    #endregion
}
