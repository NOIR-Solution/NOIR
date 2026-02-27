namespace NOIR.Domain.Events.Inventory;

/// <summary>
/// Raised when a new inventory receipt is created.
/// </summary>
public record InventoryReceiptCreatedEvent(
    Guid ReceiptId,
    string ReceiptNumber,
    InventoryReceiptType Type) : DomainEvent;

/// <summary>
/// Raised when an inventory receipt is confirmed (stock adjusted).
/// </summary>
public record InventoryReceiptConfirmedEvent(
    Guid ReceiptId,
    string ReceiptNumber,
    InventoryReceiptType Type) : DomainEvent;

/// <summary>
/// Raised when an inventory receipt is cancelled.
/// </summary>
public record InventoryReceiptCancelledEvent(
    Guid ReceiptId,
    string ReceiptNumber,
    string? CancellationReason) : DomainEvent;
