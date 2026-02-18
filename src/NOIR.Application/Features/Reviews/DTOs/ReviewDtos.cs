namespace NOIR.Application.Features.Reviews.DTOs;

/// <summary>
/// DTO for ProductReview entity.
/// </summary>
public sealed record ReviewDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string? UserName { get; init; }
    public int Rating { get; init; }
    public string? Title { get; init; }
    public string Content { get; init; } = string.Empty;
    public ReviewStatus Status { get; init; }
    public bool IsVerifiedPurchase { get; init; }
    public int HelpfulVotes { get; init; }
    public int NotHelpfulVotes { get; init; }
    public string? AdminResponse { get; init; }
    public DateTimeOffset? AdminRespondedAt { get; init; }
    public IReadOnlyList<string> MediaUrls { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Detailed DTO for a single review including media details.
/// </summary>
public sealed record ReviewDetailDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string? UserName { get; init; }
    public Guid? OrderId { get; init; }
    public int Rating { get; init; }
    public string? Title { get; init; }
    public string Content { get; init; } = string.Empty;
    public ReviewStatus Status { get; init; }
    public bool IsVerifiedPurchase { get; init; }
    public int HelpfulVotes { get; init; }
    public int NotHelpfulVotes { get; init; }
    public string? AdminResponse { get; init; }
    public DateTimeOffset? AdminRespondedAt { get; init; }
    public IReadOnlyList<ReviewMediaDto> Media { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ModifiedAt { get; init; }
}

/// <summary>
/// DTO for review media items.
/// </summary>
public sealed record ReviewMediaDto
{
    public Guid Id { get; init; }
    public string MediaUrl { get; init; } = string.Empty;
    public ReviewMediaType MediaType { get; init; }
    public int DisplayOrder { get; init; }
}

/// <summary>
/// Aggregated review statistics for a product.
/// </summary>
public sealed record ReviewStatsDto
{
    public decimal AverageRating { get; init; }
    public int TotalReviews { get; init; }
    public Dictionary<int, int> RatingDistribution { get; init; } = new();
}
