namespace NOIR.Application.Features.Inventory.DTOs;

/// <summary>
/// DTO for inventory movement history display.
/// </summary>
public sealed record InventoryMovementDto(
    Guid Id,
    Guid ProductVariantId,
    Guid ProductId,
    InventoryMovementType MovementType,
    int QuantityBefore,
    int QuantityMoved,
    int QuantityAfter,
    string? Reference,
    string? Notes,
    string? UserId,
    string? CorrelationId,
    DateTimeOffset CreatedAt);
