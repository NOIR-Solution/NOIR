namespace NOIR.Application.Features.ProductAttributes.Commands.AddProductAttributeValue;

/// <summary>
/// Command to add a value to a product attribute.
/// </summary>
public sealed record AddProductAttributeValueCommand(
    Guid AttributeId,
    string Value,
    string DisplayValue,
    string? ColorCode = null,
    string? SwatchUrl = null,
    string? IconUrl = null,
    int SortOrder = 0) : IAuditableCommand<ProductAttributeValueDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => AttributeId;
    public string? GetTargetDisplayName() => DisplayValue;
    public string? GetActionDescription() => $"Added value '{DisplayValue}' to attribute";
}
