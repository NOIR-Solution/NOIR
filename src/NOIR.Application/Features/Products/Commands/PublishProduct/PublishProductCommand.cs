namespace NOIR.Application.Features.Products.Commands.PublishProduct;

/// <summary>
/// Command to publish a product (make it active/available for purchase).
/// </summary>
public sealed record PublishProductCommand(
    Guid Id,
    string? ProductName = null) : IAuditableCommand<ProductDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => ProductName ?? Id.ToString();
    public string? GetActionDescription() => $"Published product '{GetTargetDisplayName()}'";
}
