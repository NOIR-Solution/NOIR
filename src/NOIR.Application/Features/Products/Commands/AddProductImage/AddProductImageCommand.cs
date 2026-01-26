namespace NOIR.Application.Features.Products.Commands.AddProductImage;

/// <summary>
/// Command to add an image to a product.
/// </summary>
public sealed record AddProductImageCommand(
    Guid ProductId,
    string Url,
    string? AltText,
    int SortOrder,
    bool IsPrimary) : IAuditableCommand<ProductImageDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => ProductId;
    public string? GetTargetDisplayName() => AltText ?? "Image";
    public string? GetActionDescription() => $"Added image to product";
}
