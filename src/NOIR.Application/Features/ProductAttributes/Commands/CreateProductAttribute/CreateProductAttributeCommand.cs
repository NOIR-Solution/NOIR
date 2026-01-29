namespace NOIR.Application.Features.ProductAttributes.Commands.CreateProductAttribute;

/// <summary>
/// Command to create a new product attribute.
/// </summary>
public sealed record CreateProductAttributeCommand(
    string Code,
    string Name,
    string Type,
    bool IsFilterable = false,
    bool IsSearchable = false,
    bool IsRequired = false,
    bool IsVariantAttribute = false,
    bool ShowInProductCard = false,
    bool ShowInSpecifications = true,
    bool IsGlobal = false,
    string? Unit = null,
    string? ValidationRegex = null,
    decimal? MinValue = null,
    decimal? MaxValue = null,
    int? MaxLength = null,
    string? DefaultValue = null,
    string? Placeholder = null,
    string? HelpText = null) : IAuditableCommand<ProductAttributeDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => null;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Created product attribute '{Name}'";
}
