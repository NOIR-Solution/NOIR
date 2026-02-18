using NOIR.Application.Features.Inventory.DTOs;

namespace NOIR.Application.Features.Inventory.Commands.ConfirmInventoryReceipt;

/// <summary>
/// Command to confirm an inventory receipt (adjusts stock).
/// </summary>
public sealed record ConfirmInventoryReceiptCommand(Guid ReceiptId) : IAuditableCommand<InventoryReceiptDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => ReceiptId;
    public string? GetTargetDisplayName() => "Inventory Receipt";
    public string? GetActionDescription() => "Confirmed inventory receipt";
}
