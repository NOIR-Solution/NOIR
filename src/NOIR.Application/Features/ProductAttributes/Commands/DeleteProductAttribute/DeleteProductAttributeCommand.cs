namespace NOIR.Application.Features.ProductAttributes.Commands.DeleteProductAttribute;

/// <summary>
/// Command to delete a product attribute.
/// </summary>
public sealed record DeleteProductAttributeCommand(Guid Id) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    [System.Text.Json.Serialization.JsonIgnore]
    internal string? AttributeName { get; set; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => AttributeName;
    public string? GetActionDescription() => AttributeName != null
        ? $"Deleted product attribute '{AttributeName}'"
        : $"Deleted product attribute";
}
