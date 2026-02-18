namespace NOIR.Application.Features.Promotions.Specifications;

/// <summary>
/// Specification to get a promotion by ID with related data loaded.
/// </summary>
public sealed class PromotionByIdSpec : Specification<Domain.Entities.Promotion.Promotion>
{
    public PromotionByIdSpec(Guid promotionId)
    {
        Query.Where(p => p.Id == promotionId)
            .Include(p => p.Products)
            .Include(p => p.Categories)
            .Include(p => p.Usages)
            .TagWith("PromotionById");
    }
}

/// <summary>
/// Specification to get a promotion by ID for update (with tracking).
/// </summary>
public sealed class PromotionByIdForUpdateSpec : Specification<Domain.Entities.Promotion.Promotion>
{
    public PromotionByIdForUpdateSpec(Guid promotionId)
    {
        Query.Where(p => p.Id == promotionId)
            .Include(p => p.Products)
            .Include(p => p.Categories)
            .AsTracking()
            .TagWith("PromotionByIdForUpdate");
    }
}

/// <summary>
/// Specification to get a promotion by code.
/// </summary>
public sealed class PromotionByCodeSpec : Specification<Domain.Entities.Promotion.Promotion>
{
    public PromotionByCodeSpec(string code)
    {
        Query.Where(p => p.Code == code.ToUpperInvariant())
            .Include(p => p.Products)
            .Include(p => p.Categories)
            .TagWith("PromotionByCode");
    }
}

/// <summary>
/// Specification to get a promotion by code for update (with tracking).
/// </summary>
public sealed class PromotionByCodeForUpdateSpec : Specification<Domain.Entities.Promotion.Promotion>
{
    public PromotionByCodeForUpdateSpec(string code)
    {
        Query.Where(p => p.Code == code.ToUpperInvariant())
            .Include(p => p.Products)
            .Include(p => p.Categories)
            .Include(p => p.Usages)
            .AsTracking()
            .TagWith("PromotionByCodeForUpdate");
    }
}

/// <summary>
/// Specification to filter promotions with pagination.
/// </summary>
public sealed class PromotionsFilterSpec : Specification<Domain.Entities.Promotion.Promotion>
{
    public PromotionsFilterSpec(
        int skip = 0,
        int take = 20,
        string? search = null,
        PromotionStatus? status = null,
        PromotionType? promotionType = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null)
    {
        Query.TagWith("PromotionsFilter");

        if (!string.IsNullOrEmpty(search))
            Query.Where(p => p.Name.Contains(search) || p.Code.Contains(search));

        if (status.HasValue)
            Query.Where(p => p.Status == status.Value);

        if (promotionType.HasValue)
            Query.Where(p => p.PromotionType == promotionType.Value);

        if (fromDate.HasValue)
            Query.Where(p => p.StartDate >= fromDate.Value);

        if (toDate.HasValue)
            Query.Where(p => p.EndDate <= toDate.Value);

        Query.OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take);
    }
}

/// <summary>
/// Specification to count promotions matching filter criteria.
/// </summary>
public sealed class PromotionsCountSpec : Specification<Domain.Entities.Promotion.Promotion>
{
    public PromotionsCountSpec(
        string? search = null,
        PromotionStatus? status = null,
        PromotionType? promotionType = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null)
    {
        Query.TagWith("PromotionsCount");

        if (!string.IsNullOrEmpty(search))
            Query.Where(p => p.Name.Contains(search) || p.Code.Contains(search));

        if (status.HasValue)
            Query.Where(p => p.Status == status.Value);

        if (promotionType.HasValue)
            Query.Where(p => p.PromotionType == promotionType.Value);

        if (fromDate.HasValue)
            Query.Where(p => p.StartDate >= fromDate.Value);

        if (toDate.HasValue)
            Query.Where(p => p.EndDate <= toDate.Value);
    }
}

/// <summary>
/// Specification to get currently active promotions.
/// </summary>
public sealed class ActivePromotionsSpec : Specification<Domain.Entities.Promotion.Promotion>
{
    public ActivePromotionsSpec()
    {
        var now = DateTimeOffset.UtcNow;
        Query.Where(p => p.IsActive
                && p.Status == PromotionStatus.Active
                && p.StartDate <= now
                && p.EndDate >= now)
            .OrderByDescending(p => p.CreatedAt)
            .TagWith("ActivePromotions");
    }
}

/// <summary>
/// Specification to get a promotion by code with usages loaded (for apply/validate operations).
/// </summary>
public sealed class PromotionByCodeWithUsagesSpec : Specification<Domain.Entities.Promotion.Promotion>
{
    public PromotionByCodeWithUsagesSpec(string code)
    {
        Query.Where(p => p.Code == code.ToUpperInvariant())
            .Include(p => p.Products)
            .Include(p => p.Categories)
            .Include(p => p.Usages)
            .AsTracking()
            .TagWith("PromotionByCodeWithUsages");
    }
}
