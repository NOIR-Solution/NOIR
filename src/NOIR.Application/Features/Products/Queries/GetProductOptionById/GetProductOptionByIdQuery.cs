namespace NOIR.Application.Features.Products.Queries.GetProductOptionById;

/// <summary>
/// Query to get a product option by its ID.
/// Used for before-state resolution in audit logging.
/// </summary>
public sealed record GetProductOptionByIdQuery(Guid OptionId);
