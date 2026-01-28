namespace NOIR.Application.Features.Products.Commands.DeleteProductOptionValue;

/// <summary>
/// Command to delete a product option value.
/// </summary>
public sealed record DeleteProductOptionValueCommand(
    Guid ProductId,
    Guid OptionId,
    Guid ValueId,
    string? ValueName = null) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => ValueId;
    public string? GetTargetDisplayName() => ValueName ?? ValueId.ToString();
    public string? GetActionDescription() => $"Deleted option value '{GetTargetDisplayName()}'";
}
