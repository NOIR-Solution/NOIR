namespace NOIR.Application.Features.Products.Commands.DeleteProductVariant;

/// <summary>
/// Command to delete a product variant.
/// </summary>
public sealed record DeleteProductVariantCommand(
    Guid ProductId,
    Guid VariantId,
    string? VariantName = null) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => VariantId;
    public string? GetTargetDisplayName() => VariantName ?? VariantId.ToString();
    public string? GetActionDescription() => $"Deleted variant '{GetTargetDisplayName()}'";
}
