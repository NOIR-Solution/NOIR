namespace NOIR.Application.Features.Brands.Queries.GetBrandById;

/// <summary>
/// Wolverine handler for getting a brand by ID.
/// </summary>
public class GetBrandByIdQueryHandler
{
    private readonly IRepository<Brand, Guid> _brandRepository;

    public GetBrandByIdQueryHandler(IRepository<Brand, Guid> brandRepository)
    {
        _brandRepository = brandRepository;
    }

    public async Task<Result<BrandDto>> Handle(
        GetBrandByIdQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new BrandByIdSpec(query.Id);
        var brand = await _brandRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (brand == null)
        {
            return Result.Failure<BrandDto>(
                Error.NotFound($"Brand with ID '{query.Id}' was not found.", ErrorCodes.Brand.NotFound));
        }

        return Result.Success(BrandMapper.ToDto(brand));
    }
}
