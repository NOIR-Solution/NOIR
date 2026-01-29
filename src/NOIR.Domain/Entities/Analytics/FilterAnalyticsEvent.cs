namespace NOIR.Domain.Entities.Analytics;

/// <summary>
/// Entity for tracking filter usage events.
/// Used to analyze popular filters and user behavior patterns.
/// </summary>
public class FilterAnalyticsEvent : TenantEntity<Guid>
{
    /// <summary>
    /// Session identifier for grouping events from the same browsing session.
    /// </summary>
    public string SessionId { get; private set; } = string.Empty;

    /// <summary>
    /// User ID if the user is authenticated (optional).
    /// </summary>
    public string? UserId { get; private set; }

    /// <summary>
    /// Type of filter event that occurred.
    /// </summary>
    public FilterEventType EventType { get; private set; }

    /// <summary>
    /// Category slug where the filter was applied (optional).
    /// </summary>
    public string? CategorySlug { get; private set; }

    /// <summary>
    /// Filter code/identifier (e.g., "brand", "price", "color").
    /// </summary>
    public string? FilterCode { get; private set; }

    /// <summary>
    /// Filter value that was applied (e.g., "apple", "100-500", "red").
    /// </summary>
    public string? FilterValue { get; private set; }

    /// <summary>
    /// Number of products returned after applying this filter.
    /// </summary>
    public int ProductCount { get; private set; }

    /// <summary>
    /// Search query text (for SearchPerformed events).
    /// </summary>
    public string? SearchQuery { get; private set; }

    /// <summary>
    /// Product ID that was clicked (for ProductClicked events).
    /// </summary>
    public Guid? ClickedProductId { get; private set; }

    #region Constructors

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private FilterAnalyticsEvent() : base() { }

    private FilterAnalyticsEvent(Guid id, string? tenantId) : base(id, tenantId) { }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new filter analytics event.
    /// </summary>
    public static FilterAnalyticsEvent Create(
        string sessionId,
        FilterEventType eventType,
        int productCount,
        string? tenantId = null,
        string? userId = null,
        string? categorySlug = null,
        string? filterCode = null,
        string? filterValue = null,
        string? searchQuery = null,
        Guid? clickedProductId = null)
    {
        return new FilterAnalyticsEvent(Guid.NewGuid(), tenantId)
        {
            SessionId = sessionId,
            UserId = userId,
            EventType = eventType,
            CategorySlug = categorySlug,
            FilterCode = filterCode,
            FilterValue = filterValue,
            ProductCount = productCount,
            SearchQuery = searchQuery,
            ClickedProductId = clickedProductId
        };
    }

    /// <summary>
    /// Creates a filter applied event.
    /// </summary>
    public static FilterAnalyticsEvent FilterApplied(
        string sessionId,
        string filterCode,
        string filterValue,
        int productCount,
        string? tenantId = null,
        string? userId = null,
        string? categorySlug = null)
    {
        return Create(
            sessionId: sessionId,
            eventType: FilterEventType.FilterApplied,
            productCount: productCount,
            tenantId: tenantId,
            userId: userId,
            categorySlug: categorySlug,
            filterCode: filterCode,
            filterValue: filterValue);
    }

    /// <summary>
    /// Creates a search performed event.
    /// </summary>
    public static FilterAnalyticsEvent SearchPerformed(
        string sessionId,
        string searchQuery,
        int productCount,
        string? tenantId = null,
        string? userId = null,
        string? categorySlug = null)
    {
        return Create(
            sessionId: sessionId,
            eventType: FilterEventType.SearchPerformed,
            productCount: productCount,
            tenantId: tenantId,
            userId: userId,
            categorySlug: categorySlug,
            searchQuery: searchQuery);
    }

    /// <summary>
    /// Creates a product clicked event.
    /// </summary>
    public static FilterAnalyticsEvent ProductClicked(
        string sessionId,
        Guid productId,
        string? tenantId = null,
        string? userId = null,
        string? categorySlug = null)
    {
        return Create(
            sessionId: sessionId,
            eventType: FilterEventType.ProductClicked,
            productCount: 0,
            tenantId: tenantId,
            userId: userId,
            categorySlug: categorySlug,
            clickedProductId: productId);
    }

    #endregion
}
