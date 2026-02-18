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
        receipt.Should().NotBeNull();
        receipt.Id.Should().NotBe(Guid.Empty);
        receipt.ReceiptNumber.Should().Be("RCV-20260218-0001");
        receipt.Type.Should().Be(InventoryReceiptType.StockIn);
        receipt.Status.Should().Be(InventoryReceiptStatus.Draft);
        receipt.Notes.Should().Be("Test notes");
        receipt.TenantId.Should().Be(TestTenantId);
        receipt.Items.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithStockOutType_ShouldSetCorrectType()
    {
        // Act
        var receipt = InventoryReceipt.Create("SHP-20260218-0001", InventoryReceiptType.StockOut, tenantId: TestTenantId);

        // Assert
        receipt.Type.Should().Be(InventoryReceiptType.StockOut);
        receipt.Status.Should().Be(InventoryReceiptStatus.Draft);
    }

    [Fact]
    public void Create_WithNullNotes_ShouldAllowNull()
    {
        // Act
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);

        // Assert
        receipt.Notes.Should().BeNull();
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
        item.Should().NotBeNull();
        receipt.Items.Should().HaveCount(1);
        item.ProductVariantId.Should().Be(variantId);
        item.ProductId.Should().Be(productId);
        item.ProductName.Should().Be("Test Product");
        item.VariantName.Should().Be("Size: M");
        item.Sku.Should().Be("SKU-001");
        item.Quantity.Should().Be(10);
        item.UnitCost.Should().Be(25.00m);
        item.LineTotal.Should().Be(250.00m);
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
        receipt.Items.Should().HaveCount(2);
        receipt.TotalQuantity.Should().Be(15);
        receipt.TotalCost.Should().Be(500.00m); // (10*25) + (5*50)
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot add items to a non-draft receipt.");
    }

    [Fact]
    public void AddItem_ToCancelledReceipt_ShouldThrow()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);
        receipt.Cancel("user-123", "No longer needed");

        // Act & Assert
        var act = () => receipt.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", "SKU", 10, 25.00m);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot add items to a non-draft receipt.");
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
        receipt.Status.Should().Be(InventoryReceiptStatus.Confirmed);
        receipt.ConfirmedBy.Should().Be("admin-user");
        receipt.ConfirmedAt.Should().NotBeNull();
        receipt.ConfirmedAt.Should().BeOnOrAfter(beforeConfirm);
    }

    [Fact]
    public void Confirm_EmptyReceipt_ShouldThrow()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);

        // Act & Assert
        var act = () => receipt.Confirm("admin-user");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot confirm an empty receipt.");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot confirm receipt in Confirmed status.");
    }

    [Fact]
    public void Confirm_CancelledReceipt_ShouldThrow()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);
        receipt.Cancel("user-1");

        // Act & Assert
        var act = () => receipt.Confirm("user-2");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot confirm receipt in Cancelled status.");
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
        receipt.Status.Should().Be(InventoryReceiptStatus.Cancelled);
        receipt.CancelledBy.Should().Be("admin-user");
        receipt.CancelledAt.Should().NotBeNull();
        receipt.CancelledAt.Should().BeOnOrAfter(beforeCancel);
        receipt.CancellationReason.Should().Be("No longer needed");
    }

    [Fact]
    public void Cancel_WithNullReason_ShouldAllowNull()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);

        // Act
        receipt.Cancel("admin-user");

        // Assert
        receipt.Status.Should().Be(InventoryReceiptStatus.Cancelled);
        receipt.CancellationReason.Should().BeNull();
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot cancel receipt in Confirmed status.");
    }

    [Fact]
    public void Cancel_AlreadyCancelledReceipt_ShouldThrow()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);
        receipt.Cancel("user-1");

        // Act & Assert
        var act = () => receipt.Cancel("user-2");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot cancel receipt in Cancelled status.");
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
        receipt.TotalQuantity.Should().Be(60);
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
        receipt.TotalCost.Should().Be(350.00m);
    }

    [Fact]
    public void TotalQuantity_WithNoItems_ShouldBeZero()
    {
        // Arrange
        var receipt = InventoryReceipt.Create("RCV-20260218-0001", InventoryReceiptType.StockIn, tenantId: TestTenantId);

        // Act & Assert
        receipt.TotalQuantity.Should().Be(0);
        receipt.TotalCost.Should().Be(0m);
    }

    #endregion
}
