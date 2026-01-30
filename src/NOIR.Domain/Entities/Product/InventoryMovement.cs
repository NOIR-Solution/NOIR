namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Immutable log entry for inventory movements.
/// Tracks all stock changes for audit trail and history display.
/// </summary>
public class InventoryMovement : TenantAggregateRoot<Guid>
{
    private InventoryMovement() : base() { }
    private InventoryMovement(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// The product variant whose stock changed.
    /// </summary>
    public Guid ProductVariantId { get; private set; }

    /// <summary>
    /// The parent product (denormalized for easier querying).
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Type of movement (StockIn, StockOut, Adjustment, Return, Reservation, etc.).
    /// </summary>
    public InventoryMovementType MovementType { get; private set; }

    /// <summary>
    /// Stock quantity before this movement.
    /// </summary>
    public int QuantityBefore { get; private set; }

    /// <summary>
    /// Quantity moved. Positive for inflow, negative for outflow.
    /// </summary>
    public int QuantityMoved { get; private set; }

    /// <summary>
    /// Stock quantity after this movement.
    /// </summary>
    public int QuantityAfter { get; private set; }

    /// <summary>
    /// Reference for tracing (e.g., OrderId, PO number).
    /// </summary>
    public string? Reference { get; private set; }

    /// <summary>
    /// Additional notes about the movement.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// User who initiated this movement.
    /// </summary>
    public string? UserId { get; private set; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; private set; }

    // Navigation properties
    public virtual ProductVariant ProductVariant { get; private set; } = null!;
    public virtual Product Product { get; private set; } = null!;

    /// <summary>
    /// Creates a new inventory movement record.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when movementType is not a defined enum value.</exception>
    public static InventoryMovement Create(
        Guid productVariantId,
        Guid productId,
        InventoryMovementType movementType,
        int quantityBefore,
        int quantityMoved,
        string? tenantId = null,
        string? reference = null,
        string? notes = null,
        string? userId = null,
        string? correlationId = null)
    {
        // Validate movement type is defined
        if (!Enum.IsDefined(typeof(InventoryMovementType), movementType))
        {
            throw new ArgumentException($"Invalid inventory movement type: {movementType}", nameof(movementType));
        }

        return new InventoryMovement(Guid.NewGuid(), tenantId)
        {
            ProductVariantId = productVariantId,
            ProductId = productId,
            MovementType = movementType,
            QuantityBefore = quantityBefore,
            QuantityMoved = quantityMoved,
            QuantityAfter = quantityBefore + quantityMoved,
            Reference = reference?.Length > 100 ? reference[..100] : reference,
            Notes = notes?.Length > 500 ? notes[..500] + "..." : notes,
            UserId = userId,
            CorrelationId = correlationId
        };
    }
}
