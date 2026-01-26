namespace NOIR.Application.Features.Products.Commands.CreateProductCategory;

/// <summary>
/// Command to create a new product category.
/// </summary>
public sealed record CreateProductCategoryCommand(
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

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Created product category '{Name}'";
}
