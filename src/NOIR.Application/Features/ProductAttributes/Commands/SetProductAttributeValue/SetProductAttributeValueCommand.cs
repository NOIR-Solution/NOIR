namespace NOIR.Application.Features.ProductAttributes.Commands.SetProductAttributeValue;

/// <summary>
/// Command to set a single attribute value for a product.
/// </summary>
public sealed record SetProductAttributeValueCommand(
    Guid ProductId,
    Guid AttributeId,
    Guid? VariantId,
    object? Value) : IAuditableCommand<ProductAttributeAssignmentDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    [System.Text.Json.Serialization.JsonIgnore]
    internal string? ProductName { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    internal string? AttributeName { get; set; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => ProductId;
    public string? GetTargetDisplayName() => ProductName;
    public string? GetActionDescription() => AttributeName != null && ProductName != null
        ? $"Set attribute '{AttributeName}' for product '{ProductName}'"
        : "Set product attribute value";
}
