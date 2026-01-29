namespace NOIR.Application.Features.ProductAttributes.Commands.UpdateCategoryAttribute;

/// <summary>
/// Command to update a category-attribute link settings.
/// </summary>
public sealed record UpdateCategoryAttributeCommand(
    Guid CategoryId,
    Guid AttributeId,
    bool IsRequired,
    int SortOrder) : IAuditableCommand<CategoryAttributeDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    [System.Text.Json.Serialization.JsonIgnore]
    internal string? CategoryName { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    internal string? AttributeName { get; set; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => CategoryId;
    public string? GetTargetDisplayName() => CategoryName;
    public string? GetActionDescription() => AttributeName != null && CategoryName != null
        ? $"Updated attribute '{AttributeName}' settings for category '{CategoryName}'"
        : "Updated category attribute settings";
}
