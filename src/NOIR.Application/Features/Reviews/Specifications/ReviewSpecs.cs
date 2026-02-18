namespace NOIR.Application.Features.Reviews.Specifications;

/// <summary>
/// Specification to get a review by ID with media loaded.
/// </summary>
public sealed class ReviewByIdSpec : Specification<ProductReview>
{
    public ReviewByIdSpec(Guid reviewId)
    {
        Query.Where(r => r.Id == reviewId)
            .Include(r => r.Media)
            .TagWith("ReviewById");
    }
}

/// <summary>
/// Specification to get a review by ID for update (with tracking).
/// </summary>
public sealed class ReviewByIdForUpdateSpec : Specification<ProductReview>
{
    public ReviewByIdForUpdateSpec(Guid reviewId)
    {
        Query.Where(r => r.Id == reviewId)
            .Include(r => r.Media)
            .AsTracking()
            .TagWith("ReviewByIdForUpdate");
    }
}

/// <summary>
/// Specification to check if a user already reviewed a product.
/// </summary>
public sealed class ReviewExistsSpec : Specification<ProductReview>
{
    public ReviewExistsSpec(Guid productId, string userId)
    {
        Query.Where(r => r.ProductId == productId && r.UserId == userId)
            .TagWith("ReviewExists");
    }
}

/// <summary>
/// Specification to get reviews for a product (public, approved only).
/// </summary>
public sealed class ReviewsByProductSpec : Specification<ProductReview>
{
    public ReviewsByProductSpec(
        Guid productId,
        string? sort = null,
        int skip = 0,
        int take = 20)
    {
        Query.Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved)
            .Include(r => r.Media)
            .TagWith("ReviewsByProduct");

        switch (sort?.ToLowerInvariant())
        {
            case "highest":
                Query.OrderByDescending(r => r.Rating);
                break;
            case "lowest":
                Query.OrderBy(r => r.Rating);
                break;
            case "mosthelpful":
                Query.OrderByDescending(r => r.HelpfulVotes);
                break;
            default: // newest
                Query.OrderByDescending(r => r.CreatedAt);
                break;
        }

        Query.Skip(skip).Take(take);
    }
}

/// <summary>
/// Specification to count approved reviews for a product.
/// </summary>
public sealed class ReviewsByProductCountSpec : Specification<ProductReview>
{
    public ReviewsByProductCountSpec(Guid productId)
    {
        Query.Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved)
            .TagWith("ReviewsByProductCount");
    }
}

/// <summary>
/// Specification for admin moderation queue with filters and pagination.
/// </summary>
public sealed class ReviewsModerationListSpec : Specification<ProductReview>
{
    public ReviewsModerationListSpec(
        ReviewStatus? status = null,
        Guid? productId = null,
        int? rating = null,
        string? search = null,
        int skip = 0,
        int take = 20)
    {
        Query.Include(r => r.Media)
            .TagWith("ReviewsModerationList");

        if (status.HasValue)
            Query.Where(r => r.Status == status.Value);

        if (productId.HasValue)
            Query.Where(r => r.ProductId == productId.Value);

        if (rating.HasValue)
            Query.Where(r => r.Rating == rating.Value);

        if (!string.IsNullOrEmpty(search))
            Query.Where(r => r.Content.Contains(search) || (r.Title != null && r.Title.Contains(search)));

        Query.OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take);
    }
}

/// <summary>
/// Specification to count reviews matching moderation filters.
/// </summary>
public sealed class ReviewsModerationCountSpec : Specification<ProductReview>
{
    public ReviewsModerationCountSpec(
        ReviewStatus? status = null,
        Guid? productId = null,
        int? rating = null,
        string? search = null)
    {
        Query.TagWith("ReviewsModerationCount");

        if (status.HasValue)
            Query.Where(r => r.Status == status.Value);

        if (productId.HasValue)
            Query.Where(r => r.ProductId == productId.Value);

        if (rating.HasValue)
            Query.Where(r => r.Rating == rating.Value);

        if (!string.IsNullOrEmpty(search))
            Query.Where(r => r.Content.Contains(search) || (r.Title != null && r.Title.Contains(search)));
    }
}

/// <summary>
/// Specification to get reviews by user.
/// </summary>
public sealed class ReviewsByUserSpec : Specification<ProductReview>
{
    public ReviewsByUserSpec(string userId)
    {
        Query.Where(r => r.UserId == userId)
            .Include(r => r.Media)
            .OrderByDescending(r => r.CreatedAt)
            .TagWith("ReviewsByUser");
    }
}

/// <summary>
/// Specification for aggregating review stats for a product.
/// Returns all approved reviews for a product (used for in-memory aggregation).
/// </summary>
public sealed class ReviewStatsSpec : Specification<ProductReview>
{
    public ReviewStatsSpec(Guid productId)
    {
        Query.Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved)
            .TagWith("ReviewStats");
    }
}

/// <summary>
/// Specification to get multiple reviews by IDs for bulk operations (with tracking).
/// </summary>
public sealed class ReviewsByIdsForUpdateSpec : Specification<ProductReview>
{
    public ReviewsByIdsForUpdateSpec(List<Guid> reviewIds)
    {
        Query.Where(r => reviewIds.Contains(r.Id))
            .AsTracking()
            .TagWith("ReviewsByIdsForUpdate");
    }
}
