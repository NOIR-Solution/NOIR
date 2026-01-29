namespace NOIR.Application.Features.ProductAttributes.Commands.RemoveProductAttributeValue;

/// <summary>
/// Command to remove a value from a product attribute.
/// </summary>
public sealed record RemoveProductAttributeValueCommand(
    Guid AttributeId,
    Guid ValueId) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    [System.Text.Json.Serialization.JsonIgnore]
    internal string? ValueDisplayName { get; set; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => ValueId;
    public string? GetTargetDisplayName() => ValueDisplayName;
    public string? GetActionDescription() => ValueDisplayName != null
        ? $"Removed attribute value '{ValueDisplayName}'"
        : "Removed attribute value";
}
