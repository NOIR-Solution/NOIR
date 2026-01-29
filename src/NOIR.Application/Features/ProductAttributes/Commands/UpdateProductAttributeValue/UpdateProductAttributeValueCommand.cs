namespace NOIR.Application.Features.ProductAttributes.Commands.UpdateProductAttributeValue;

/// <summary>
/// Command to update a product attribute value.
/// </summary>
public sealed record UpdateProductAttributeValueCommand(
    Guid AttributeId,
    Guid ValueId,
    string Value,
    string DisplayValue,
    string? ColorCode,
    string? SwatchUrl,
    string? IconUrl,
    int SortOrder,
    bool IsActive) : IAuditableCommand<ProductAttributeValueDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => ValueId;
    public string? GetTargetDisplayName() => DisplayValue;
    public string? GetActionDescription() => $"Updated attribute value '{DisplayValue}'";
}
