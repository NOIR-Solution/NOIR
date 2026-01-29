namespace NOIR.Application.Features.Products.Queries.GetProductOptionValueById;

/// <summary>
/// Query to get a product option value by its ID.
/// Used for before-state resolution in audit logging.
/// </summary>
public sealed record GetProductOptionValueByIdQuery(Guid ValueId);
