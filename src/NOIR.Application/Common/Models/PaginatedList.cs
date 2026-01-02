namespace NOIR.Application.Common.Models;

/// <summary>
/// Represents a paginated list of items.
/// </summary>
/// <typeparam name="T">The type of items in the list.</typeparam>
public class PaginatedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }

    /// <summary>
    /// Primary constructor for JSON deserialization and direct instantiation.
    /// Parameter names match JSON property names for proper deserialization.
    /// </summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public PaginatedList(IReadOnlyList<T> items, int pageNumber, int totalPages, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        TotalPages = totalPages;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Factory method for creating PaginatedList from count and pageSize.
    /// </summary>
    public static PaginatedList<T> Create(IReadOnlyList<T> items, int count, int pageNumber, int pageSize)
    {
        var totalPages = (int)Math.Ceiling(count / (double)pageSize);
        return new PaginatedList<T>(items, pageNumber, totalPages, count);
    }

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var count = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Create(items, count, pageNumber, pageSize);
    }
}
