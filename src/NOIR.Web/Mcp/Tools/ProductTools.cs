using System.ComponentModel;
using ModelContextProtocol.Server;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Queries.GetProductById;
using NOIR.Application.Features.Products.Queries.SearchProductVariants;
using NOIR.Web.Mcp.Filters;
using NOIR.Web.Mcp.Helpers;
using NOIR.Application.Features.Products.Queries.GetProducts;

namespace NOIR.Web.Mcp.Tools;

/// <summary>
/// MCP tools for product catalog management.
/// </summary>
[McpServerToolType]
[RequiresModule(ModuleNames.Ecommerce.Products)]
public sealed class ProductTools(IMessageBus bus)
{
    [McpServerTool(Name = "noir_products_list", ReadOnly = true, Idempotent = true)]
    [Description("List products with pagination and filtering. Supports search, status, category, brand, price range, and stock filters.")]
    public async Task<PagedResult<ProductListDto>> ListProducts(
        [Description("Search by product name or SKU")] string? search = null,
        [Description("Filter by status: Draft, Active, Archived, OutOfStock")] string? status = null,
        [Description("Filter by category ID (GUID)")] string? categoryId = null,
        [Description("Filter by brand name")] string? brand = null,
        [Description("Minimum price filter")] decimal? minPrice = null,
        [Description("Maximum price filter")] decimal? maxPrice = null,
        [Description("Only show products in stock")] bool? inStockOnly = null,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Page size, max 100 (default: 20)")] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var productStatus = status is not null && Enum.TryParse<ProductStatus>(status, true, out var s) ? s : (ProductStatus?)null;
        var catId = categoryId is not null ? Guid.Parse(categoryId) : (Guid?)null;

        var result = await bus.InvokeAsync<Result<PagedResult<ProductListDto>>>(
            new GetProductsQuery(search, productStatus, catId, brand, minPrice, maxPrice, inStockOnly, null, page, pageSize), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_products_get", ReadOnly = true, Idempotent = true)]
    [Description("Get full product details by ID or slug, including variants, images, attributes, and pricing.")]
    public async Task<ProductDto> GetProduct(
        [Description("Product ID (GUID) — provide either id or slug")] string? id = null,
        [Description("Product URL slug — provide either id or slug")] string? slug = null,
        CancellationToken ct = default)
    {
        var productId = id is not null ? Guid.Parse(id) : (Guid?)null;
        var result = await bus.InvokeAsync<Result<ProductDto>>(
            new GetProductByIdQuery(productId, slug), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_products_search_variants", ReadOnly = true, Idempotent = true)]
    [Description("Search product variants by SKU, name, or barcode. Useful for inventory and order operations.")]
    public async Task<PagedResult<ProductVariantDto>> SearchVariants(
        [Description("Search by variant SKU, product name, or barcode")] string? search = null,
        [Description("Filter by category ID (GUID)")] string? categoryId = null,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Page size, max 100 (default: 20)")] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var catId = categoryId is not null ? Guid.Parse(categoryId) : (Guid?)null;

        var result = await bus.InvokeAsync<Result<PagedResult<ProductVariantDto>>>(
            new SearchProductVariantsQuery(search, catId, page, pageSize), ct);
        return result.Unwrap();
    }
}
