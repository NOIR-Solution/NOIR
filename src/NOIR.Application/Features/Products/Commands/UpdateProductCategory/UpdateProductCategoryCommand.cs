namespace NOIR.Application.Features.Products.Commands.UpdateProductCategory;

/// <summary>
/// Command to update an existing product category.
/// </summary>
public sealed record UpdateProductCategoryCommand(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? MetaTitle,
    string? MetaDescription,
    string? ImageUrl,
    int SortOrder,
    Guid? ParentId) : IAuditableCommand<ProductCategoryDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Updated product category '{Name}'";
}
