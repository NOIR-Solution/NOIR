namespace NOIR.Application.Features.Products.Commands.DuplicateProduct;

/// <summary>
/// Command to duplicate a product.
/// Creates a new draft product with optional copying of variants, images, and options.
/// </summary>
public sealed record DuplicateProductCommand(
    Guid Id,
    bool CopyVariants = false,
    bool CopyImages = false,
    bool CopyOptions = false,
    string? ProductName = null) : IAuditableCommand<ProductDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => ProductName;
    public string? GetActionDescription() => $"Duplicated product '{GetTargetDisplayName()}'";
}
