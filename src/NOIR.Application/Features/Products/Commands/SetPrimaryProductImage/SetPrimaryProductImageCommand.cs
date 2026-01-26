namespace NOIR.Application.Features.Products.Commands.SetPrimaryProductImage;

/// <summary>
/// Command to set a product image as primary.
/// </summary>
public sealed record SetPrimaryProductImageCommand(
    Guid ProductId,
    Guid ImageId) : IAuditableCommand<ProductDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => ProductId;
    public string? GetTargetDisplayName() => ProductId.ToString();
    public string? GetActionDescription() => "Set primary product image";
}
