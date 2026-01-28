namespace NOIR.Application.Features.Products.Commands.UpdateProductOptionValue;

/// <summary>
/// Command to update a product option value.
/// </summary>
public sealed record UpdateProductOptionValueCommand(
    Guid ProductId,
    Guid OptionId,
    Guid ValueId,
    string Value,
    string? DisplayValue,
    string? ColorCode,
    string? SwatchUrl,
    int SortOrder) : IAuditableCommand<ProductOptionValueDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => ProductId;
    public string? GetTargetDisplayName() => DisplayValue ?? Value;
    public string? GetActionDescription() => $"Updated option value '{DisplayValue ?? Value}'";
}
