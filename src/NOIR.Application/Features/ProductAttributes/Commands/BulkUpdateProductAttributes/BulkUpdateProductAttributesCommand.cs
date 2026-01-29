namespace NOIR.Application.Features.ProductAttributes.Commands.BulkUpdateProductAttributes;

/// <summary>
/// Command to bulk update multiple attribute values for a product.
/// </summary>
public sealed record BulkUpdateProductAttributesCommand(
    Guid ProductId,
    Guid? VariantId,
    IReadOnlyCollection<AttributeValueItem> Values) : IAuditableCommand<IReadOnlyCollection<ProductAttributeAssignmentDto>>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    [System.Text.Json.Serialization.JsonIgnore]
    internal string? ProductName { get; set; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => ProductId;
    public string? GetTargetDisplayName() => ProductName;
    public string? GetActionDescription() => ProductName != null
        ? $"Updated {Values?.Count ?? 0} attributes for product '{ProductName}'"
        : $"Updated {Values?.Count ?? 0} product attributes";
}
