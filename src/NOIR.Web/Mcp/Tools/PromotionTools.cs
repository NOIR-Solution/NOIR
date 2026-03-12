using System.ComponentModel;
using ModelContextProtocol.Server;
using NOIR.Application.Features.Promotions.DTOs;
using NOIR.Application.Features.Promotions.Queries.GetPromotionById;
using NOIR.Application.Features.Promotions.Queries.GetPromotions;
using NOIR.Web.Mcp.Filters;
using NOIR.Web.Mcp.Helpers;

namespace NOIR.Web.Mcp.Tools;

/// <summary>
/// MCP tools for promotion/discount code management.
/// </summary>
[McpServerToolType]
[RequiresModule(ModuleNames.Ecommerce.Promotions)]
public sealed class PromotionTools(IMessageBus bus)
{
    [McpServerTool(Name = "noir_promotions_list", ReadOnly = true, Idempotent = true)]
    [Description("List promotions with pagination and filtering. Supports search, status, type, and date range filters.")]
    public async Task<PagedResult<PromotionDto>> ListPromotions(
        [Description("Search by promotion name or code")] string? search = null,
        [Description("Filter by status: Draft, Active, Scheduled, Expired, Cancelled")] string? status = null,
        [Description("Filter by type: VoucherCode, FlashSale, BundleDeal, FreeShipping")] string? promotionType = null,
        [Description("Filter promotions active from this date (ISO 8601)")] string? fromDate = null,
        [Description("Filter promotions active to this date (ISO 8601)")] string? toDate = null,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Page size, max 100 (default: 20)")] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var promoStatus = status is not null && Enum.TryParse<PromotionStatus>(status, true, out var s) ? s : (PromotionStatus?)null;
        var promoType = promotionType is not null && Enum.TryParse<PromotionType>(promotionType, true, out var t) ? t : (PromotionType?)null;
        var from = fromDate is not null ? DateTimeOffset.Parse(fromDate) : (DateTimeOffset?)null;
        var to = toDate is not null ? DateTimeOffset.Parse(toDate) : (DateTimeOffset?)null;

        var result = await bus.InvokeAsync<Result<PagedResult<PromotionDto>>>(
            new GetPromotionsQuery(page, pageSize, search, promoStatus, promoType, from, to), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_promotions_get", ReadOnly = true, Idempotent = true)]
    [Description("Get full promotion details by ID, including discount rules, usage limits, and applicable products/categories.")]
    public async Task<PromotionDto> GetPromotion(
        [Description("The promotion ID (GUID)")] string promotionId,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<PromotionDto>>(
            new GetPromotionByIdQuery(Guid.Parse(promotionId)), ct);
        return result.Unwrap();
    }
}
