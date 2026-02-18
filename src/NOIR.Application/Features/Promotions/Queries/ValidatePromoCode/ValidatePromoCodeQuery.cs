namespace NOIR.Application.Features.Promotions.Queries.ValidatePromoCode;

/// <summary>
/// Query to validate a promotion code and calculate the potential discount.
/// </summary>
public sealed record ValidatePromoCodeQuery(
    string Code,
    decimal OrderTotal,
    string? UserId = null);
