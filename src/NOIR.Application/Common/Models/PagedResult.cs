using System.Text.Json.Serialization;

namespace NOIR.Application.Common.Models;

/// <summary>
/// Represents a paginated result set with metadata.
/// Used by repository methods that support pagination.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// The items in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Current page index (0-based).
    /// </summary>
    public int PageIndex { get; }

    /// <summary>
    /// Current page number (1-based, for display/API compatibility).
    /// </summary>
    public int PageNumber => PageIndex + 1;

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages { get; }

    /// <summary>
    /// Whether there's a previous page.
    /// </summary>
    public bool HasPreviousPage => PageIndex > 0;

    /// <summary>
    /// Whether there's a next page.
    /// </summary>
    public bool HasNextPage => PageIndex < TotalPages - 1;

    /// <summary>
    /// The first item number on this page (1-based, for display).
    /// </summary>
    public int FirstItemOnPage => TotalCount == 0 ? 0 : PageIndex * PageSize + 1;

    /// <summary>
    /// The last item number on this page (1-based, for display).
    /// </summary>
    public int LastItemOnPage => Math.Min((PageIndex + 1) * PageSize, TotalCount);

    /// <summary>
    /// JSON deserialization constructor.
    /// </summary>
    [JsonConstructor]
    public PagedResult(IReadOnlyList<T> items, int totalCount, int pageIndex, int pageSize, int totalPages)
    {
        Items = items;
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalPages = totalPages;
    }

    private PagedResult(IReadOnlyList<T> items, int totalCount, int pageIndex, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
    }

    /// <summary>
    /// Creates a new paged result.
    /// </summary>
    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int pageIndex, int pageSize)
    {
        return new PagedResult<T>(items, totalCount, pageIndex, pageSize);
    }

    /// <summary>
    /// Creates an empty paged result.
    /// </summary>
    public static PagedResult<T> Empty(int pageIndex = 0, int pageSize = 10)
    {
        return new PagedResult<T>([], 0, pageIndex, pageSize);
    }

    /// <summary>
    /// Maps items to a different type.
    /// </summary>
    public PagedResult<TDestination> Map<TDestination>(Func<T, TDestination> mapper)
    {
        var mappedItems = Items.Select(mapper).ToList();
        return PagedResult<TDestination>.Create(mappedItems, TotalCount, PageIndex, PageSize);
    }
}

/// <summary>
/// Extension methods for creating paged results.
/// </summary>
public static class PagedResultExtensions
{
    /// <summary>
    /// Converts a list to a paged result (for in-memory pagination).
    /// Note: Prefer database-level pagination for large datasets.
    /// </summary>
    public static PagedResult<T> ToPagedResult<T>(this IEnumerable<T> source, int pageIndex, int pageSize)
    {
        var list = source.ToList();
        var totalCount = list.Count;
        var items = list.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        return PagedResult<T>.Create(items, totalCount, pageIndex, pageSize);
    }
}
