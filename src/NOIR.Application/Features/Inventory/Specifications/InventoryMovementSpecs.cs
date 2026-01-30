using NOIR.Domain.Entities.Product;

namespace NOIR.Application.Features.Inventory.Specifications;

/// <summary>
/// Specification to retrieve stock history for a specific variant with pagination.
/// Uses AsNoTracking (default) - InventoryMovement records are immutable audit logs.
/// </summary>
public sealed class StockHistoryByVariantIdSpec : Specification<InventoryMovement>
{
    public StockHistoryByVariantIdSpec(
        Guid productId,
        Guid variantId,
        int? skip = null,
        int? take = null)
    {
        // AsNoTracking is default - InventoryMovement is immutable
        Query.Where(m => m.ProductId == productId && m.ProductVariantId == variantId)
             .OrderByDescending(m => m.CreatedAt)
             .ThenByDescending(m => m.Id);

        if (skip.HasValue)
        {
            Query.Skip(skip.Value);
        }

        if (take.HasValue)
        {
            Query.Take(take.Value);
        }

        Query.TagWith("GetStockHistoryByVariantId");
    }
}

/// <summary>
/// Count specification for stock history pagination.
/// Uses AsNoTracking (default) - read-only count query.
/// </summary>
public sealed class StockHistoryByVariantIdCountSpec : Specification<InventoryMovement>
{
    public StockHistoryByVariantIdCountSpec(Guid productId, Guid variantId)
    {
        Query.Where(m => m.ProductId == productId && m.ProductVariantId == variantId)
             .TagWith("CountStockHistoryByVariantId");
    }
}
