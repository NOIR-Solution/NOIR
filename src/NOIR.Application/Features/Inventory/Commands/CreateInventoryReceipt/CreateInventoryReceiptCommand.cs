using NOIR.Application.Features.Inventory.DTOs;

namespace NOIR.Application.Features.Inventory.Commands.CreateInventoryReceipt;

/// <summary>
/// Command to create a new inventory receipt (draft).
/// </summary>
public sealed record CreateInventoryReceiptCommand(
    InventoryReceiptType Type,
    string? Notes,
    List<CreateInventoryReceiptItemDto> Items) : IAuditableCommand<InventoryReceiptDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => null;
    public string? GetTargetDisplayName() => "Inventory Receipt";
    public string? GetActionDescription() => $"Created {Type} inventory receipt";
}
