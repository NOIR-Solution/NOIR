namespace NOIR.Application.Features.Promotions.Queries.GetPromotionById;

/// <summary>
/// Wolverine handler for getting a promotion by ID.
/// </summary>
public class GetPromotionByIdQueryHandler
{
    private readonly IRepository<Domain.Entities.Promotion.Promotion, Guid> _repository;

    public GetPromotionByIdQueryHandler(IRepository<Domain.Entities.Promotion.Promotion, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<Result<PromotionDto>> Handle(
        GetPromotionByIdQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new Promotions.Specifications.PromotionByIdSpec(query.Id);
        var promotion = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (promotion is null)
        {
            return Result.Failure<PromotionDto>(
                Error.NotFound($"Promotion with ID '{query.Id}' not found.", "NOIR-PROMO-002"));
        }

        return Result.Success(PromotionMapper.ToDto(promotion));
    }
}
