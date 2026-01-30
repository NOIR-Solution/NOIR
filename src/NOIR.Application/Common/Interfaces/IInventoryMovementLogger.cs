using NOIR.Domain.Entities.Product;

namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for logging inventory movements for audit trail.
/// </summary>
public interface IInventoryMovementLogger
{
    /// <summary>
    /// Logs an inventory movement for a product variant.
    /// </summary>
    Task LogMovementAsync(
        ProductVariant variant,
        InventoryMovementType movementType,
        int quantityBefore,
        int quantityMoved,
        string? reference = null,
        string? notes = null,
        string? userId = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
