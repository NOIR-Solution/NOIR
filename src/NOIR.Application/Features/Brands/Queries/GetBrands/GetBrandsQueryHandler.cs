namespace NOIR.Application.Features.Brands.Queries.GetBrands;

/// <summary>
/// Wolverine handler for getting paged list of brands.
/// </summary>
public class GetBrandsQueryHandler
{
    private readonly IRepository<Brand, Guid> _brandRepository;

    public GetBrandsQueryHandler(IRepository<Brand, Guid> brandRepository)
    {
        _brandRepository = brandRepository;
    }

    public async Task<Result<PagedResult<BrandListDto>>> Handle(
        GetBrandsQuery query,
        CancellationToken cancellationToken)
    {
        // Get paged brands
        var spec = new BrandsPagedSpec(
            query.Search,
            query.IsActive,
            query.IsFeatured,
            query.Page,
            query.PageSize);

        var brands = await _brandRepository.ListAsync(spec, cancellationToken);

        // Get total count
        var countSpec = new BrandsCountSpec(
            query.Search,
            query.IsActive,
            query.IsFeatured);

        var totalCount = await _brandRepository.CountAsync(countSpec, cancellationToken);

        var items = brands.Select(BrandMapper.ToListDto).ToList();

        // PagedResult expects pageIndex (0-based), but query.Page is 1-based
        var pageIndex = query.Page - 1;

        return Result.Success(PagedResult<BrandListDto>.Create(
            items,
            totalCount,
            pageIndex,
            query.PageSize));
    }
}
