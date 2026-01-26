namespace NOIR.Application.Features.Products.Queries.GetProductCategoryById;

/// <summary>
/// Wolverine handler for getting a single product category by ID.
/// </summary>
public class GetProductCategoryByIdQueryHandler
{
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;

    public GetProductCategoryByIdQueryHandler(IRepository<ProductCategory, Guid> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<ProductCategoryDto>> Handle(
        GetProductCategoryByIdQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new ProductCategoryByIdSpec(query.Id);
        var category = await _categoryRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (category is null)
        {
            return Result.Failure<ProductCategoryDto>(
                Error.NotFound("Product category not found.", "NOIR-PRODUCT-003"));
        }

        var children = category.Children?
            .Select(c => ProductMapper.ToDto(c, null))
            .ToList();

        return Result.Success(ProductMapper.ToDtoWithChildren(category, children));
    }
}
