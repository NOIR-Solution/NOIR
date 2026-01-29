namespace NOIR.Application.Features.Products.Commands.UpdateProductOption;

/// <summary>
/// Command to update a product option.
/// </summary>
public sealed record UpdateProductOptionCommand(
    Guid ProductId,
    Guid OptionId,
    string Name,
    string? DisplayName,
    int SortOrder) : IAuditableCommand<ProductOptionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => OptionId;
    public string? GetTargetDisplayName() => DisplayName ?? Name;
    public string? GetActionDescription() => $"Updated option '{DisplayName ?? Name}'";
}
