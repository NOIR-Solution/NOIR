namespace NOIR.Application.Features.Brands.Commands.UpdateBrand;

/// <summary>
/// Command to update an existing brand.
/// </summary>
public sealed record UpdateBrandCommand(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? Website,
    string? LogoUrl,
    string? BannerUrl,
    string? MetaTitle,
    string? MetaDescription,
    bool IsActive,
    bool IsFeatured,
    int SortOrder) : IAuditableCommand<BrandDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => Id;
    public AuditOperationType OperationType => AuditOperationType.Update;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Updated brand '{Name}'";
}
