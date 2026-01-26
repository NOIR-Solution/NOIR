namespace NOIR.Application.Features.Products.Commands.UpdateProductImage;

/// <summary>
/// Command to update a product image.
/// </summary>
public sealed record UpdateProductImageCommand(
    Guid ProductId,
    Guid ImageId,
    string Url,
    string? AltText,
    int SortOrder) : IAuditableCommand<ProductImageDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => ImageId;
    public string? GetTargetDisplayName() => AltText ?? "Image";
    public string? GetActionDescription() => $"Updated product image";
}
