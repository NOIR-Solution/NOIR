namespace NOIR.Application.Features.Brands;

/// <summary>
/// Mapper for Brand entity to DTOs.
/// </summary>
public static class BrandMapper
{
    /// <summary>
    /// Maps a Brand entity to a full BrandDto.
    /// </summary>
    public static BrandDto ToDto(Brand brand) => new(
        brand.Id,
        brand.Name,
        brand.Slug,
        brand.LogoUrl,
        brand.BannerUrl,
        brand.Description,
        brand.Website,
        brand.MetaTitle,
        brand.MetaDescription,
        brand.IsActive,
        brand.IsFeatured,
        brand.SortOrder,
        brand.ProductCount,
        brand.CreatedAt,
        brand.ModifiedAt);

    /// <summary>
    /// Maps a Brand entity to a BrandListDto.
    /// </summary>
    public static BrandListDto ToListDto(Brand brand) => new(
        brand.Id,
        brand.Name,
        brand.Slug,
        brand.LogoUrl,
        brand.IsActive,
        brand.IsFeatured,
        brand.ProductCount);
}
