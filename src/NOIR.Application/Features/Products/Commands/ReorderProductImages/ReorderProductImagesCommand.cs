namespace NOIR.Application.Features.Products.Commands.ReorderProductImages;

/// <summary>
/// Command to reorder product images in bulk.
/// Updates the sort order of multiple images in a single request.
/// </summary>
public sealed record ReorderProductImagesCommand(
    Guid ProductId,
    List<ImageSortOrderItem> Items) : IAuditableCommand<ProductDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => ProductId;
    public string? GetTargetDisplayName() => $"Reordered {Items.Count} images";
    public string? GetActionDescription() => $"Reordered {Items.Count} product images";
}
