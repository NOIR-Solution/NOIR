using NOIR.Application.Features.FilterAnalytics.DTOs;
using NOIR.Domain.Entities.Analytics;

namespace NOIR.Application.Features.FilterAnalytics.Queries.GetPopularFilters;

/// <summary>
/// Handler for getting popular filters query.
/// </summary>
public class GetPopularFiltersQueryHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<GetPopularFiltersQueryHandler> _logger;

    public GetPopularFiltersQueryHandler(
        IApplicationDbContext dbContext,
        ILogger<GetPopularFiltersQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<PopularFiltersResult>> Handle(
        GetPopularFiltersQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Getting popular filters: FromDate={FromDate}, ToDate={ToDate}, Category={Category}, Top={Top}",
            query.FromDate,
            query.ToDate,
            query.CategorySlug,
            query.Top);

        // Default date range: last 30 days
        var toDate = query.ToDate ?? DateTimeOffset.UtcNow;
        var fromDate = query.FromDate ?? toDate.AddDays(-30);

        var eventsQuery = _dbContext.FilterAnalyticsEvents
            .TagWith("GetPopularFilters")
            .AsNoTracking()
            .Where(e => e.EventType == FilterEventType.FilterApplied)
            .Where(e => e.CreatedAt >= fromDate && e.CreatedAt <= toDate);

        // Filter by category if specified
        if (!string.IsNullOrWhiteSpace(query.CategorySlug))
        {
            eventsQuery = eventsQuery.Where(e => e.CategorySlug == query.CategorySlug);
        }

        // Count total events
        var totalEvents = await eventsQuery.CountAsync(cancellationToken);

        // Get popular filters grouped by code and value
        var popularFilters = await eventsQuery
            .Where(e => e.FilterCode != null)
            .GroupBy(e => new { e.FilterCode, e.FilterValue, e.CategorySlug })
            .Select(g => new
            {
                FilterCode = g.Key.FilterCode!,
                FilterValue = g.Key.FilterValue,
                CategorySlug = g.Key.CategorySlug,
                UsageCount = g.Count(),
                UniqueUsers = g.Where(e => e.UserId != null).Select(e => e.UserId).Distinct().Count()
            })
            .OrderByDescending(x => x.UsageCount)
            .Take(query.Top)
            .ToListAsync(cancellationToken);

        // Calculate conversion rate (clicks / filter applications)
        var clicksByFilter = await _dbContext.FilterAnalyticsEvents
            .TagWith("GetPopularFilters.Clicks")
            .AsNoTracking()
            .Where(e => e.EventType == FilterEventType.ProductClicked)
            .Where(e => e.CreatedAt >= fromDate && e.CreatedAt <= toDate)
            .Where(e => !string.IsNullOrEmpty(query.CategorySlug) ? e.CategorySlug == query.CategorySlug : true)
            .GroupBy(e => e.SessionId)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);

        var sessionsWithClicks = clicksByFilter.ToHashSet();

        var result = new PopularFiltersResult
        {
            Filters = popularFilters.Select(f =>
            {
                // Simple conversion rate based on unique users
                var conversionRate = f.UniqueUsers > 0
                    ? Math.Round((decimal)f.UsageCount / f.UniqueUsers, 2)
                    : 0;

                return new PopularFilterDto(
                    FilterCode: f.FilterCode,
                    FilterValue: f.FilterValue,
                    CategorySlug: f.CategorySlug,
                    UsageCount: f.UsageCount,
                    UniqueUsers: f.UniqueUsers,
                    ConversionRate: conversionRate);
            }).ToList(),
            TotalEvents = totalEvents,
            FromDate = fromDate,
            ToDate = toDate
        };

        _logger.LogDebug(
            "Found {FilterCount} popular filters from {TotalEvents} events",
            result.Filters.Count,
            totalEvents);

        return Result.Success(result);
    }
}
