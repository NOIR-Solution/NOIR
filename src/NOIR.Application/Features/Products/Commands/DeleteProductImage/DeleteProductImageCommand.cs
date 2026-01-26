namespace NOIR.Application.Features.Products.Commands.DeleteProductImage;

/// <summary>
/// Command to delete a product image.
/// </summary>
public sealed record DeleteProductImageCommand(
    Guid ProductId,
    Guid ImageId) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => ImageId;
    public string? GetTargetDisplayName() => ImageId.ToString();
    public string? GetActionDescription() => "Deleted product image";
}
