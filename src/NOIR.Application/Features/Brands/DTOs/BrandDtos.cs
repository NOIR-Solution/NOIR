namespace NOIR.Application.Features.Brands.DTOs;

/// <summary>
/// Full brand details DTO.
/// </summary>
public sealed record BrandDto(
    Guid Id,
    string Name,
    string Slug,
    string? LogoUrl,
    string? BannerUrl,
    string? Description,
    string? Website,
    string? MetaTitle,
    string? MetaDescription,
    bool IsActive,
    bool IsFeatured,
    int SortOrder,
    int ProductCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// Brand list item DTO (minimal data for listings).
/// </summary>
public sealed record BrandListDto(
    Guid Id,
    string Name,
    string Slug,
    string? LogoUrl,
    bool IsActive,
    bool IsFeatured,
    int ProductCount);

/// <summary>
/// Request to create a new brand.
/// </summary>
public sealed record CreateBrandRequest(
    string Name,
    string Slug,
    string? LogoUrl,
    string? BannerUrl,
    string? Description,
    string? Website,
    string? MetaTitle,
    string? MetaDescription,
    bool IsFeatured = false);

/// <summary>
/// Request to update an existing brand.
/// </summary>
public sealed record UpdateBrandRequest(
    string Name,
    string Slug,
    string? LogoUrl,
    string? BannerUrl,
    string? Description,
    string? Website,
    string? MetaTitle,
    string? MetaDescription,
    bool IsActive,
    bool IsFeatured,
    int SortOrder = 0);
