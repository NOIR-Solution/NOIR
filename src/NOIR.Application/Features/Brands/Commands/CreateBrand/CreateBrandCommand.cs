namespace NOIR.Application.Features.Brands.Commands.CreateBrand;

/// <summary>
/// Command to create a new brand.
/// </summary>
public sealed record CreateBrandCommand(
    string Name,
    string Slug,
    string? LogoUrl,
    string? BannerUrl,
    string? Description,
    string? Website,
    string? MetaTitle,
    string? MetaDescription,
    bool IsFeatured = false) : IAuditableCommand<BrandDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => null;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Created brand '{Name}'";
}
