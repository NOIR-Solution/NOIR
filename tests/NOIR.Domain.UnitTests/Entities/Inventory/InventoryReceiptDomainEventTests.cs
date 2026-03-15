using NOIR.Domain.Entities.Inventory;
using NOIR.Domain.Events.Inventory;

namespace NOIR.Domain.UnitTests.Entities.Inventory;

/// <summary>
/// Unit tests verifying that the InventoryReceipt aggregate root raises
/// the correct domain events for creation, confirmation, and cancellation.
/// </summary>
public class InventoryReceiptDomainEventTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "user-123";

    private static InventoryReceipt CreateTestReceipt(
        string receiptNumber = "RCV-20260226-0001",
        InventoryReceiptType type = InventoryReceiptType.StockIn,
        string? notes = null,
        string? tenantId = TestTenantId)
    {
        return InventoryReceipt.Create(receiptNumber, type, notes, tenantId);
    }

    /// <summary>
    /// Adds a valid item to the receipt so it can be confirmed.
    /// </summary>
    private static void AddTestItem(InventoryReceipt receipt)
    {
        receipt.AddItem(
            productVariantId: Guid.NewGuid(),
            productId: Guid.NewGuid(),
            productName: "Test Product",
            variantName: "Default",
            sku: "SKU-001",
            quantity: 10,
            unitCost: 50_000m);
    }

    #region Create Domain Event

    [Fact]
    public void Create_ShouldRaiseInventoryReceiptCreatedEvent()
    {
        // Act
        var receipt = CreateTestReceipt();

        // Assert
        receipt.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<InventoryReceiptCreatedEvent>();
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectReceiptId()
    {
        // Act
        var receipt = CreateTestReceipt();

        // Assert
        var domainEvent = receipt.DomainEvents.OfType<InventoryReceiptCreatedEvent>().Single();
        domainEvent.ReceiptId.ShouldBe(receipt.Id);
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectReceiptNumber()
    {
        // Act
        var receipt = CreateTestReceipt(receiptNumber: "SHP-20260226-0005");

        // Assert
        var domainEvent = receipt.DomainEvents.OfType<InventoryReceiptCreatedEvent>().Single();
        domainEvent.ReceiptNumber.ShouldBe("SHP-20260226-0005");
    }

    [Fact]
    public void Create_StockIn_ShouldRaiseEventWithCorrectType()
    {
        // Act
        var receipt = CreateTestReceipt(type: InventoryReceiptType.StockIn);

        // Assert
        var domainEvent = receipt.DomainEvents.OfType<InventoryReceiptCreatedEvent>().Single();
        domainEvent.Type.ShouldBe(InventoryReceiptType.StockIn);
    }

    [Fact]
    public void Create_StockOut_ShouldRaiseEventWithCorrectType()
    {
        // Act
        var receipt = CreateTestReceipt(type: InventoryReceiptType.StockOut);

        // Assert
        var domainEvent = receipt.DomainEvents.OfType<InventoryReceiptCreatedEvent>().Single();
        domainEvent.Type.ShouldBe(InventoryReceiptType.StockOut);
    }

    #endregion

    #region Confirm Domain Event

    [Fact]
    public void Confirm_ShouldRaiseInventoryReceiptConfirmedEvent()
    {
        // Arrange
        var receipt = CreateTestReceipt();
        AddTestItem(receipt);
        receipt.ClearDomainEvents();

        // Act
        receipt.Confirm(TestUserId);

        // Assert
        receipt.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<InventoryReceiptConfirmedEvent>();
    }

    [Fact]
    public void Confirm_ShouldRaiseEventWithCorrectProperties()
    {
        // Arrange
        var receipt = CreateTestReceipt(receiptNumber: "RCV-20260226-0002", type: InventoryReceiptType.StockIn);
        AddTestItem(receipt);
        receipt.ClearDomainEvents();

        // Act
        receipt.Confirm(TestUserId);

        // Assert
        var domainEvent = receipt.DomainEvents.OfType<InventoryReceiptConfirmedEvent>().Single();
        domainEvent.ReceiptId.ShouldBe(receipt.Id);
        domainEvent.ReceiptNumber.ShouldBe("RCV-20260226-0002");
        domainEvent.Type.ShouldBe(InventoryReceiptType.StockIn);
    }

    #endregion

    #region Cancel Domain Event

    [Fact]
    public void Cancel_ShouldRaiseInventoryReceiptCancelledEvent()
    {
        // Arrange
        var receipt = CreateTestReceipt();
        receipt.ClearDomainEvents();

        // Act
        receipt.Cancel(TestUserId, "Wrong items");

        // Assert
        receipt.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<InventoryReceiptCancelledEvent>();
    }

    [Fact]
    public void Cancel_ShouldRaiseEventWithCorrectProperties()
    {
        // Arrange
        var receipt = CreateTestReceipt(receiptNumber: "RCV-20260226-0003");
        receipt.ClearDomainEvents();

        // Act
        receipt.Cancel(TestUserId, "Duplicate receipt");

        // Assert
        var domainEvent = receipt.DomainEvents.OfType<InventoryReceiptCancelledEvent>().Single();
        domainEvent.ReceiptId.ShouldBe(receipt.Id);
        domainEvent.ReceiptNumber.ShouldBe("RCV-20260226-0003");
        domainEvent.CancellationReason.ShouldBe("Duplicate receipt");
    }

    [Fact]
    public void Cancel_WithNullReason_ShouldRaiseEventWithNullReason()
    {
        // Arrange
        var receipt = CreateTestReceipt();
        receipt.ClearDomainEvents();

        // Act
        receipt.Cancel(TestUserId);

        // Assert
        var domainEvent = receipt.DomainEvents.OfType<InventoryReceiptCancelledEvent>().Single();
        domainEvent.CancellationReason.ShouldBeNull();
    }

    #endregion
}
