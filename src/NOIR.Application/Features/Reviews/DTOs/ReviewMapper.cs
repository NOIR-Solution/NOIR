namespace NOIR.Application.Features.Reviews.DTOs;

/// <summary>
/// Mapper for Review-related entities to DTOs.
/// </summary>
public static class ReviewMapper
{
    /// <summary>
    /// Maps a ProductReview entity to ReviewDto.
    /// </summary>
    public static ReviewDto ToDto(ProductReview review, string? productName = null, string? userName = null)
    {
        return new ReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            ProductName = productName,
            UserId = review.UserId,
            UserName = userName,
            Rating = review.Rating,
            Title = review.Title,
            Content = review.Content,
            Status = review.Status,
            IsVerifiedPurchase = review.IsVerifiedPurchase,
            HelpfulVotes = review.HelpfulVotes,
            NotHelpfulVotes = review.NotHelpfulVotes,
            AdminResponse = review.AdminResponse,
            AdminRespondedAt = review.AdminRespondedAt,
            MediaUrls = review.Media.OrderBy(m => m.DisplayOrder).Select(m => m.MediaUrl).ToList(),
            CreatedAt = review.CreatedAt
        };
    }

    /// <summary>
    /// Maps a ProductReview entity to ReviewDetailDto.
    /// </summary>
    public static ReviewDetailDto ToDetailDto(ProductReview review, string? productName = null, string? userName = null)
    {
        return new ReviewDetailDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            ProductName = productName,
            UserId = review.UserId,
            UserName = userName,
            OrderId = review.OrderId,
            Rating = review.Rating,
            Title = review.Title,
            Content = review.Content,
            Status = review.Status,
            IsVerifiedPurchase = review.IsVerifiedPurchase,
            HelpfulVotes = review.HelpfulVotes,
            NotHelpfulVotes = review.NotHelpfulVotes,
            AdminResponse = review.AdminResponse,
            AdminRespondedAt = review.AdminRespondedAt,
            Media = review.Media.OrderBy(m => m.DisplayOrder).Select(ToMediaDto).ToList(),
            CreatedAt = review.CreatedAt,
            ModifiedAt = review.ModifiedAt
        };
    }

    /// <summary>
    /// Maps a ReviewMedia entity to ReviewMediaDto.
    /// </summary>
    public static ReviewMediaDto ToMediaDto(ReviewMedia media)
    {
        return new ReviewMediaDto
        {
            Id = media.Id,
            MediaUrl = media.MediaUrl,
            MediaType = media.MediaType,
            DisplayOrder = media.DisplayOrder
        };
    }
}
