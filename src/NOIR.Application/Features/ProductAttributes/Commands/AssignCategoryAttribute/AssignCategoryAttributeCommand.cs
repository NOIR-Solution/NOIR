namespace NOIR.Application.Features.ProductAttributes.Commands.AssignCategoryAttribute;

/// <summary>
/// Command to assign an attribute to a category.
/// </summary>
public sealed record AssignCategoryAttributeCommand(
    Guid CategoryId,
    Guid AttributeId,
    bool IsRequired = false,
    int SortOrder = 0) : IAuditableCommand<CategoryAttributeDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    [System.Text.Json.Serialization.JsonIgnore]
    internal string? CategoryName { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    internal string? AttributeName { get; set; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => CategoryId;
    public string? GetTargetDisplayName() => CategoryName;
    public string? GetActionDescription() => AttributeName != null && CategoryName != null
        ? $"Assigned attribute '{AttributeName}' to category '{CategoryName}'"
        : "Assigned attribute to category";
}
