namespace NOIR.Application.Features.Products.Commands.AddProductOptionValue;

/// <summary>
/// Command to add a value to a product option.
/// </summary>
public sealed record AddProductOptionValueCommand(
    Guid ProductId,
    Guid OptionId,
    string Value,
    string? DisplayValue,
    string? ColorCode,
    string? SwatchUrl,
    int SortOrder) : IAuditableCommand<ProductOptionValueDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => ProductId;
    public string? GetTargetDisplayName() => DisplayValue ?? Value;
    public string? GetActionDescription() => $"Added option value '{DisplayValue ?? Value}'";
}
