namespace NOIR.Application.Features.Promotions.Queries.GetPromotionById;

/// <summary>
/// Query to get a promotion by ID with full details.
/// </summary>
public sealed record GetPromotionByIdQuery(Guid Id);
