namespace NOIR.Application.Features.Promotions.Queries.GetPromotions;

/// <summary>
/// Wolverine handler for getting promotions with pagination.
/// </summary>
public class GetPromotionsQueryHandler
{
    private readonly IRepository<Domain.Entities.Promotion.Promotion, Guid> _repository;

    public GetPromotionsQueryHandler(IRepository<Domain.Entities.Promotion.Promotion, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<PromotionDto>>> Handle(
        GetPromotionsQuery query,
        CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        // Get total count
        var countSpec = new Promotions.Specifications.PromotionsCountSpec(
            query.Search,
            query.Status,
            query.PromotionType,
            query.FromDate,
            query.ToDate);
        var totalCount = await _repository.CountAsync(countSpec, cancellationToken);

        // Get promotions
        var listSpec = new Promotions.Specifications.PromotionsFilterSpec(
            skip,
            query.PageSize,
            query.Search,
            query.Status,
            query.PromotionType,
            query.FromDate,
            query.ToDate);
        var promotions = await _repository.ListAsync(listSpec, cancellationToken);

        var items = promotions.Select(PromotionMapper.ToDto).ToList();

        var pageIndex = query.Page - 1;
        return Result.Success(PagedResult<PromotionDto>.Create(
            items,
            totalCount,
            pageIndex,
            query.PageSize));
    }
}
