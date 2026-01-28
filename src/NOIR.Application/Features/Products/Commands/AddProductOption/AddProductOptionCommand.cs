namespace NOIR.Application.Features.Products.Commands.AddProductOption;

/// <summary>
/// Command to add an option type to a product.
/// </summary>
public sealed record AddProductOptionCommand(
    Guid ProductId,
    string Name,
    string? DisplayName,
    int SortOrder,
    List<AddProductOptionValueRequest>? Values) : IAuditableCommand<ProductOptionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => ProductId;
    public string? GetTargetDisplayName() => DisplayName ?? Name;
    public string? GetActionDescription() => $"Added option '{DisplayName ?? Name}' to product";
}
