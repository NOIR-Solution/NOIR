namespace NOIR.Application.Features.Promotions.Commands.ApplyPromotion;

/// <summary>
/// Wolverine handler for applying a promotion code to an order.
/// </summary>
public class ApplyPromotionCommandHandler
{
    private readonly IRepository<Domain.Entities.Promotion.Promotion, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ApplyPromotionCommandHandler(
        IRepository<Domain.Entities.Promotion.Promotion, Guid> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PromotionUsageDto>> Handle(
        ApplyPromotionCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.UserId))
        {
            return Result.Failure<PromotionUsageDto>(
                Error.Validation("UserId", "User must be authenticated to apply promotions.", "NOIR-PROMO-005"));
        }

        // Get promotion with usages loaded for tracking
        var spec = new Promotions.Specifications.PromotionByCodeWithUsagesSpec(command.Code);
        var promotion = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (promotion is null)
        {
            return Result.Failure<PromotionUsageDto>(
                Error.NotFound($"Promotion with code '{command.Code}' not found.", "NOIR-PROMO-006"));
        }

        // Check if promotion is valid
        if (!promotion.IsValid())
        {
            return Result.Failure<PromotionUsageDto>(
                Error.Validation("Code", "This promotion is not currently valid.", "NOIR-PROMO-007"));
        }

        // Check per-user usage limit
        var userUsageCount = promotion.Usages.Count(u => u.UserId == command.UserId);
        if (!promotion.CanBeUsedBy(command.UserId, userUsageCount))
        {
            return Result.Failure<PromotionUsageDto>(
                Error.Validation("Code", "You have reached the usage limit for this promotion.", "NOIR-PROMO-008"));
        }

        // Check minimum order value
        if (promotion.MinOrderValue.HasValue && command.OrderTotal < promotion.MinOrderValue.Value)
        {
            return Result.Failure<PromotionUsageDto>(
                Error.Validation("OrderTotal",
                    $"Minimum order value of {promotion.MinOrderValue.Value:N0} is required for this promotion.",
                    "NOIR-PROMO-009"));
        }

        // Calculate discount
        var discountAmount = promotion.CalculateDiscount(command.OrderTotal);

        // Record usage
        var usage = new Domain.Entities.Promotion.PromotionUsage(
            Guid.NewGuid(),
            promotion.Id,
            command.UserId,
            command.OrderId,
            discountAmount,
            promotion.TenantId);

        promotion.Usages.Add(usage);
        promotion.IncrementUsage();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(PromotionMapper.ToUsageDto(usage));
    }
}
