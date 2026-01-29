namespace NOIR.Application.Features.ProductAttributes.Commands.UpdateProductAttribute;

/// <summary>
/// Command to update an existing product attribute.
/// </summary>
public sealed record UpdateProductAttributeCommand(
    Guid Id,
    string Code,
    string Name,
    bool IsFilterable,
    bool IsSearchable,
    bool IsRequired,
    bool IsVariantAttribute,
    bool ShowInProductCard,
    bool ShowInSpecifications,
    bool IsGlobal,
    string? Unit,
    string? ValidationRegex,
    decimal? MinValue,
    decimal? MaxValue,
    int? MaxLength,
    string? DefaultValue,
    string? Placeholder,
    string? HelpText,
    int SortOrder,
    bool IsActive) : IAuditableCommand<ProductAttributeDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Updated product attribute '{Name}'";
}
