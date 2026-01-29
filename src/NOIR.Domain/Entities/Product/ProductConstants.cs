namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Constants for product-related thresholds and configuration.
/// Shared between backend and frontend for consistency.
/// </summary>
public static class ProductConstants
{
    /// <summary>
    /// Stock quantity threshold for "low stock" warning.
    /// Products with stock > 0 and stock less than this value are considered low stock.
    /// </summary>
    public const int LowStockThreshold = 10;

    /// <summary>
    /// Maximum number of products allowed per bulk operation.
    /// </summary>
    public const int MaxBulkOperationSize = 1000;
}
