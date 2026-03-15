using NOIR.Domain.Entities.Inventory;

namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the InventoryReceipt aggregate root entity.
/// Tests factory methods, item management, confirmation workflow, and cancellation.
/// </summary>
public class InventoryReceiptTests
{
    private const string TestTenantId = "test-tenant";

    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidReceipt()
    {
        // Arrange & Act
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, "Test notes", TestTenantId);

        // Assert
        receipt.ShouldNotBeNull();
        receipt.Id.ShouldNotBe(Guid.Empty);
        receipt.ReceiptNumber.ShouldBe("RCV-20260218-0001");
        receipt.Type.ShouldBe(InventoryReceiptType.StockIn);
        receipt.Status.ShouldBe(InventoryReceiptStatus.Draft);
        receipt.Notes.ShouldBe("Test notes");
        receipt.TenantId.ShouldBe(TestTenantId);
        receipt.Items.ShouldBeEmpty();
    }

    [Fact]
    public void Create_WithStockOutType_ShouldSetCorrectType()
    {
        // Act
        var receipt = InventoryReceipt.Create("SHP-20260218-0001", InventoryReceiptType.StockOut, tenantId: TestTenantId);

        // Assert
        receipt.Type.ShouldBe(InventoryReceiptType.StockOut);
        receipt.Status.ShouldBe(InventoryReceiptStatus.Draft);
    }

    [Fact]
    public void Create_WithNullNotes_ShouldAllowNull()
    {
        // Act
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);

        // Assert
        receipt.Notes.ShouldBeNull();
    }

    #endregion

    #region AddItem Tests

    [Fact]
    public void AddItem_ToDraftReceipt_ShouldAddItemSuccessfully()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // Act
        var item = receipt.AddItem(variantId, productId, "Test Product", "Size: M", "SKU-001", 10, 25.00m);

        // Assert
        item.ShouldNotBeNull();
        receipt.Items.Count().ShouldBe(1);
        item.ProductVariantId.ShouldBe(variantId);
        item.ProductId.ShouldBe(productId);
        item.ProductName.ShouldBe("Test Product");
        item.VariantName.ShouldBe("Size: M");
        item.Sku.ShouldBe("SKU-001");
        item.Quantity.ShouldBe(10);
        item.UnitCost.ShouldBe(25.00m);
        item.LineTotal.ShouldBe(250.00m);
    }

    [Fact]
    public void AddItem_MultipleItems_ShouldTrackAllItems()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);

        // Act
        receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product 1", "Variant 1", "SKU-001", 10, 25.00m);
        receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product 2", "Variant 2", "SKU-002", 5, 50.00m);

        // Assert
        receipt.Items.Count().ShouldBe(2);
        receipt.TotalQuantity.ShouldBe(15);
        receipt.TotalCost.ShouldBe(500.00m); // (10*25) + (5*50)
    }

    [Fact]
    public void AddItem_ToConfirmedReceipt_ShouldThrow()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);
        receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product 1", "Variant 1", "SKU-001", 10, 25.00m);
        receipt.Confirm("user-123");

        // Act & Assert
        var act = () => receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product 2", "Variant 2", "SKU-002", 5, 50.00m);
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot add items to a non-draft receipt.");
    }

    [Fact]
    public void AddItem_ToCancelledReceipt_ShouldThrow()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);
        receipt.Cancel("user-123", "No longer needed");

        // Act & Assert
        var act = () => receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", "SKU", 10, 25.00m);
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot add items to a non-draft receipt.");
    }

    #endregion

    #region Confirm Tests

    [Fact]
    public void Confirm_DraftReceiptWithItems_ShouldConfirmSuccessfully()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);
        receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product 1", "Variant 1", "SKU-001", 10, 25.00m);
        var beforeConfirm = DateTimeOffset.UtcNow;

        // Act
        receipt.Confirm("admin-user");

        // Assert
        receipt.Status.ShouldBe(InventoryReceiptStatus.Confirmed);
        receipt.ConfirmedBy.ShouldBe("admin-user");
        receipt.ConfirmedAt.ShouldNotBeNull();
        receipt.ConfirmedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeConfirm);
    }

    [Fact]
    public void Confirm_EmptyReceipt_ShouldThrow()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);

        // Act & Assert
        var act = () => receipt.Confirm("admin-user");
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot confirm an empty receipt.");
    }

    [Fact]
    public void Confirm_AlreadyConfirmedReceipt_ShouldThrow()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);
        receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", "SKU", 10, 25.00m);
        receipt.Confirm("user-1");

        // Act & Assert
        var act = () => receipt.Confirm("user-2");
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot confirm receipt in Confirmed status.");
    }

    [Fact]
    public void Confirm_CancelledReceipt_ShouldThrow()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);
        receipt.Cancel("user-1");

        // Act & Assert
        var act = () => receipt.Confirm("user-2");
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot confirm receipt in Cancelled status.");
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_DraftReceipt_ShouldCancelSuccessfully()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);
        var beforeCancel = DateTimeOffset.UtcNow;

        // Act
        receipt.Cancel("admin-user", "No longer needed");

        // Assert
        receipt.Status.ShouldBe(InventoryReceiptStatus.Cancelled);
        receipt.CancelledBy.ShouldBe("admin-user");
        receipt.CancelledAt.ShouldNotBeNull();
        receipt.CancelledAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeCancel);
        receipt.CancellationReason.ShouldBe("No longer needed");
    }

    [Fact]
    public void Cancel_WithNullReason_ShouldAllowNull()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);

        // Act
        receipt.Cancel("admin-user");

        // Assert
        receipt.Status.ShouldBe(InventoryReceiptStatus.Cancelled);
        receipt.CancellationReason.ShouldBeNull();
    }

    [Fact]
    public void Cancel_ConfirmedReceipt_ShouldThrow()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);
        receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", "SKU", 10, 25.00m);
        receipt.Confirm("user-1");

        // Act & Assert
        var act = () => receipt.Cancel("user-2", "Changed mind");
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot cancel receipt in Confirmed status.");
    }

    [Fact]
    public void Cancel_AlreadyCancelledReceipt_ShouldThrow()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);
        receipt.Cancel("user-1");

        // Act & Assert
        var act = () => receipt.Cancel("user-2");
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot cancel receipt in Cancelled status.");
    }

    #endregion

    #region Computed Properties

    [Fact]
    public void TotalQuantity_WithMultipleItems_ShouldSumCorrectly()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);
        receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "P1", "V1", "S1", 10, 10.00m);
        receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "P2", "V2", "S2", 20, 20.00m);
        receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "P3", "V3", "S3", 30, 30.00m);

        // Act & Assert
        receipt.TotalQuantity.ShouldBe(60);
    }

    [Fact]
    public void TotalCost_WithMultipleItems_ShouldSumLineTotalsCorrectly()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);
        receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "P1", "V1", "S1", 10, 10.00m); // 100
        receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "P2", "V2", "S2", 5, 20.00m);  // 100
        receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "P3", "V3", "S3", 3, 50.00m);  // 150

        // Act & Assert
        receipt.TotalCost.ShouldBe(350.00m);
    }

    [Fact]
    public void TotalQuantity_WithNoItems_ShouldBeZero()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);

        // Act & Assert
        receipt.TotalQuantity.ShouldBe(0);
        receipt.TotalCost.ShouldBe(0m);
    }

    #endregion
}
