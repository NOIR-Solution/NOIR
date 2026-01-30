using NOIR.Application.Features.Inventory.DTOs;
using NOIR.Domain.Entities.Product;

namespace NOIR.Application.Features.Inventory.Mappers;

/// <summary>
/// Mapper for InventoryMovement entity to DTO conversions.
/// </summary>
public static class InventoryMovementMapper
{
    public static InventoryMovementDto ToDto(InventoryMovement movement) => new(
        movement.Id,
        movement.ProductVariantId,
        movement.ProductId,
        movement.MovementType,
        movement.QuantityBefore,
        movement.QuantityMoved,
        movement.QuantityAfter,
        movement.Reference,
        movement.Notes,
        movement.UserId,
        movement.CorrelationId,
        movement.CreatedAt);
}
