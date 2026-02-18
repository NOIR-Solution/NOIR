namespace NOIR.Application.Features.Promotions.Queries.ValidatePromoCode;

/// <summary>
/// Wolverine handler for validating a promotion code.
/// </summary>
public class ValidatePromoCodeQueryHandler
{
    private readonly IRepository<Domain.Entities.Promotion.Promotion, Guid> _repository;

    public ValidatePromoCodeQueryHandler(IRepository<Domain.Entities.Promotion.Promotion, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<Result<PromoCodeValidationDto>> Handle(
        ValidatePromoCodeQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new Promotions.Specifications.PromotionByCodeSpec(query.Code);
        var promotion = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (promotion is null)
        {
            return Result.Success(new PromoCodeValidationDto
            {
                IsValid = false,
                Message = "Invalid promotion code.",
                Code = query.Code
            });
        }

        if (!promotion.IsValid())
        {
            var reason = promotion.Status switch
            {
                PromotionStatus.Expired => "This promotion has expired.",
                PromotionStatus.Cancelled => "This promotion has been cancelled.",
                PromotionStatus.Draft => "This promotion is not yet active.",
                PromotionStatus.Scheduled => "This promotion is not yet active.",
                _ when !promotion.IsActive => "This promotion is not currently active.",
                _ when promotion.UsageLimitTotal.HasValue && promotion.CurrentUsageCount >= promotion.UsageLimitTotal.Value
                    => "This promotion has reached its usage limit.",
                _ => "This promotion is not currently valid."
            };

            return Result.Success(new PromoCodeValidationDto
            {
                IsValid = false,
                Message = reason,
                Code = query.Code,
                DiscountType = promotion.DiscountType,
                DiscountValue = promotion.DiscountValue
            });
        }

        // Check minimum order value
        if (promotion.MinOrderValue.HasValue && query.OrderTotal < promotion.MinOrderValue.Value)
        {
            return Result.Success(new PromoCodeValidationDto
            {
                IsValid = false,
                Message = $"Minimum order value of {promotion.MinOrderValue.Value:N0} is required.",
                Code = query.Code,
                DiscountType = promotion.DiscountType,
                DiscountValue = promotion.DiscountValue
            });
        }

        // Check per-user limit if user is provided
        if (!string.IsNullOrEmpty(query.UserId) && promotion.UsageLimitPerUser.HasValue)
        {
            // Load usages to check user-specific count
            var usagesSpec = new Promotions.Specifications.PromotionByIdSpec(promotion.Id);
            var promotionWithUsages = await _repository.FirstOrDefaultAsync(usagesSpec, cancellationToken);
            if (promotionWithUsages is not null)
            {
                var userUsageCount = promotionWithUsages.Usages.Count(u => u.UserId == query.UserId);
                if (userUsageCount >= promotion.UsageLimitPerUser.Value)
                {
                    return Result.Success(new PromoCodeValidationDto
                    {
                        IsValid = false,
                        Message = "You have reached the usage limit for this promotion.",
                        Code = query.Code,
                        DiscountType = promotion.DiscountType,
                        DiscountValue = promotion.DiscountValue
                    });
                }
            }
        }

        var discountAmount = promotion.CalculateDiscount(query.OrderTotal);

        return Result.Success(new PromoCodeValidationDto
        {
            IsValid = true,
            Message = "Promotion code is valid.",
            DiscountAmount = discountAmount,
            Code = query.Code,
            DiscountType = promotion.DiscountType,
            DiscountValue = promotion.DiscountValue,
            MaxDiscountAmount = promotion.MaxDiscountAmount
        });
    }
}
