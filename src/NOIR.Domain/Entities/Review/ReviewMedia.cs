namespace NOIR.Domain.Entities.Review;

/// <summary>
/// Media (image or video) attached to a product review.
/// </summary>
public class ReviewMedia : TenantEntity<Guid>
{
    public Guid ReviewId { get; private set; }
    public string MediaUrl { get; private set; } = string.Empty;
    public ReviewMediaType MediaType { get; private set; }
    public int DisplayOrder { get; private set; }

    // Navigation
    public virtual ProductReview Review { get; private set; } = null!;

    // Private constructor for EF Core
    private ReviewMedia() { }

    /// <summary>
    /// Factory method to create a new review media item.
    /// </summary>
    internal static ReviewMedia Create(
        Guid reviewId,
        string mediaUrl,
        ReviewMediaType mediaType,
        int displayOrder,
        string? tenantId)
    {
        return new ReviewMedia
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReviewId = reviewId,
            MediaUrl = mediaUrl,
            MediaType = mediaType,
            DisplayOrder = displayOrder
        };
    }
}
