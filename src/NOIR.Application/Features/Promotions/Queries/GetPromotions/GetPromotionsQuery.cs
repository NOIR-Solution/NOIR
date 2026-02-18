namespace NOIR.Application.Features.Promotions.Queries.GetPromotions;

/// <summary>
/// Query to get promotions with pagination and filtering.
/// </summary>
public sealed record GetPromotionsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    PromotionStatus? Status = null,
    PromotionType? PromotionType = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null);
