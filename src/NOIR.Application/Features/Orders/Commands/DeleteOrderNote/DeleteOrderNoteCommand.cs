namespace NOIR.Application.Features.Orders.Commands.DeleteOrderNote;

/// <summary>
/// Command to delete an order note.
/// </summary>
public sealed record DeleteOrderNoteCommand(
    Guid OrderId,
    Guid NoteId) : IAuditableCommand<OrderNoteDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => NoteId;
    public string? GetTargetDisplayName() => "Order Note";
    public string? GetActionDescription() => "Deleted note from order";
}
