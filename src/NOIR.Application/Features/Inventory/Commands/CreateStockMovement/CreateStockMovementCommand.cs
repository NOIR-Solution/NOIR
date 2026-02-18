using NOIR.Application.Features.Inventory.DTOs;

namespace NOIR.Application.Features.Inventory.Commands.CreateStockMovement;

/// <summary>
/// Command to create a manual stock movement (StockIn, StockOut, or Adjustment).
/// </summary>
public sealed record CreateStockMovementCommand(
    Guid ProductId,
    Guid ProductVariantId,
    InventoryMovementType MovementType,
    int Quantity,
    string? Reference,
    string? Notes) : IAuditableCommand<InventoryMovementDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => null;
    public string? GetTargetDisplayName() => "Inventory Movement";
    public string? GetActionDescription() => $"Created {MovementType} movement for quantity {Quantity}";
}
