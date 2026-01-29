namespace NOIR.Application.Features.FilterAnalytics.DTOs;

/// <summary>
/// DTO representing a filter analytics event.
/// </summary>
public sealed record FilterAnalyticsEventDto(
    Guid Id,
    string SessionId,
    string? UserId,
    FilterEventType EventType,
    string? CategorySlug,
    string? FilterCode,
    string? FilterValue,
    int ProductCount,
    string? SearchQuery,
    Guid? ClickedProductId,
    DateTimeOffset CreatedAt);

/// <summary>
/// DTO representing a popular filter with usage statistics.
/// </summary>
public sealed record PopularFilterDto(
    string FilterCode,
    string? FilterValue,
    string? CategorySlug,
    int UsageCount,
    int UniqueUsers,
    decimal ConversionRate);

/// <summary>
/// Result of popular filters query.
/// </summary>
public sealed record PopularFiltersResult
{
    public List<PopularFilterDto> Filters { get; init; } = new();
    public int TotalEvents { get; init; }
    public DateTimeOffset FromDate { get; init; }
    public DateTimeOffset ToDate { get; init; }
}
