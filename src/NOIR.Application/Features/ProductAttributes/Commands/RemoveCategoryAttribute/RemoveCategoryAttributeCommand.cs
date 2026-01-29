namespace NOIR.Application.Features.ProductAttributes.Commands.RemoveCategoryAttribute;

/// <summary>
/// Command to remove an attribute from a category.
/// </summary>
public sealed record RemoveCategoryAttributeCommand(
    Guid CategoryId,
    Guid AttributeId) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    [System.Text.Json.Serialization.JsonIgnore]
    internal string? CategoryName { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    internal string? AttributeName { get; set; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => CategoryId;
    public string? GetTargetDisplayName() => CategoryName;
    public string? GetActionDescription() => AttributeName != null && CategoryName != null
        ? $"Removed attribute '{AttributeName}' from category '{CategoryName}'"
        : "Removed attribute from category";
}
