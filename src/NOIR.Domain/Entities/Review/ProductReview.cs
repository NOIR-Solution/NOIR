namespace NOIR.Domain.Entities.Review;

/// <summary>
/// Represents a customer review of a product.
/// </summary>
public class ProductReview : TenantAggregateRoot<Guid>
{
    public Guid ProductId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public Guid? OrderId { get; private set; }

    // Review content
    public int Rating { get; private set; }
    public string? Title { get; private set; }
    public string Content { get; private set; } = string.Empty;

    // Status
    public ReviewStatus Status { get; private set; }
    public bool IsVerifiedPurchase { get; private set; }

    // Votes
    public int HelpfulVotes { get; private set; }
    public int NotHelpfulVotes { get; private set; }

    // Admin response
    public string? AdminResponse { get; private set; }
    public DateTimeOffset? AdminRespondedAt { get; private set; }

    // Navigation
    public virtual ICollection<ReviewMedia> Media { get; private set; } = new List<ReviewMedia>();

    // Private constructor for EF Core
    private ProductReview() : base() { }

    private ProductReview(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new product review.
    /// </summary>
    public static ProductReview Create(
        Guid productId,
        string userId,
        int rating,
        string? title,
        string content,
        Guid? orderId = null,
        bool isVerifiedPurchase = false,
        string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");

        return new ProductReview(Guid.NewGuid(), tenantId)
        {
            ProductId = productId,
            UserId = userId,
            Rating = rating,
            Title = title?.Trim(),
            Content = content.Trim(),
            OrderId = orderId,
            IsVerifiedPurchase = isVerifiedPurchase,
            Status = ReviewStatus.Pending,
            HelpfulVotes = 0,
            NotHelpfulVotes = 0
        };
    }

    /// <summary>
    /// Approves the review for public display.
    /// </summary>
    public void Approve()
    {
        Status = ReviewStatus.Approved;
    }

    /// <summary>
    /// Rejects the review.
    /// </summary>
    public void Reject()
    {
        Status = ReviewStatus.Rejected;
    }

    /// <summary>
    /// Adds an admin response to the review.
    /// </summary>
    public void AddAdminResponse(string response)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(response);

        AdminResponse = response.Trim();
        AdminRespondedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Increments the helpful vote count.
    /// </summary>
    public void VoteHelpful()
    {
        HelpfulVotes++;
    }

    /// <summary>
    /// Increments the not-helpful vote count.
    /// </summary>
    public void VoteNotHelpful()
    {
        NotHelpfulVotes++;
    }

    /// <summary>
    /// Adds a media item to the review.
    /// </summary>
    public ReviewMedia AddMedia(string mediaUrl, ReviewMediaType mediaType, int displayOrder)
    {
        var media = ReviewMedia.Create(Id, mediaUrl, mediaType, displayOrder, TenantId);
        Media.Add(media);
        return media;
    }
}
