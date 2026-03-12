using System.ComponentModel;
using ModelContextProtocol.Server;
using NOIR.Web.Mcp.Helpers;

namespace NOIR.Web.Mcp.Tools;

/// <summary>
/// MCP tools for global search across all entity types.
/// </summary>
[McpServerToolType]
public sealed class SearchTools(IMessageBus bus)
{
    [McpServerTool(Name = "noir_search_global", ReadOnly = true, Idempotent = true)]
    [Description("Search across all entity types: products, orders, customers, blog posts, and users. Returns up to MaxPerCategory results per category. Minimum 2 characters required.")]
    public async Task<GlobalSearchResponseDto> GlobalSearch(
        [Description("Search query string (minimum 2 characters)")] string query,
        [Description("Maximum results per category (default: 5)")] int maxPerCategory = 5,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new GlobalSearchResponseDto([], [], [], [], [], 0);

        var result = await bus.InvokeAsync<Result<GlobalSearchResponseDto>>(
            new GlobalSearchQuery(query, maxPerCategory), ct);
        return result.Unwrap();
    }
}
