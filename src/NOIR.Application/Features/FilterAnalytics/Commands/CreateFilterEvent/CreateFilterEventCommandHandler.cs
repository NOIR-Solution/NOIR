using NOIR.Application.Features.FilterAnalytics.DTOs;
using NOIR.Domain.Entities.Analytics;

namespace NOIR.Application.Features.FilterAnalytics.Commands.CreateFilterEvent;

/// <summary>
/// Handler for creating filter analytics events.
/// Uses IApplicationDbContext directly since FilterAnalyticsEvent is a TenantEntity (not AggregateRoot).
/// </summary>
public class CreateFilterEventCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CreateFilterEventCommandHandler> _logger;

    public CreateFilterEventCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        ILogger<CreateFilterEventCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<FilterAnalyticsEventDto>> Handle(
        CreateFilterEventCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Creating filter event: {EventType} for session {SessionId}",
            command.EventType,
            command.SessionId);

        var tenantId = _currentUser.TenantId;
        var userId = _currentUser.IsAuthenticated ? _currentUser.UserId : null;

        var analyticsEvent = FilterAnalyticsEvent.Create(
            sessionId: command.SessionId,
            eventType: command.EventType,
            productCount: command.ProductCount,
            tenantId: tenantId,
            userId: userId,
            categorySlug: command.CategorySlug,
            filterCode: command.FilterCode,
            filterValue: command.FilterValue,
            searchQuery: command.SearchQuery,
            clickedProductId: command.ClickedProductId);

        _dbContext.FilterAnalyticsEvents.Add(analyticsEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Created filter event {EventId}: {EventType}",
            analyticsEvent.Id,
            command.EventType);

        var dto = new FilterAnalyticsEventDto(
            Id: analyticsEvent.Id,
            SessionId: analyticsEvent.SessionId,
            UserId: analyticsEvent.UserId,
            EventType: analyticsEvent.EventType,
            CategorySlug: analyticsEvent.CategorySlug,
            FilterCode: analyticsEvent.FilterCode,
            FilterValue: analyticsEvent.FilterValue,
            ProductCount: analyticsEvent.ProductCount,
            SearchQuery: analyticsEvent.SearchQuery,
            ClickedProductId: analyticsEvent.ClickedProductId,
            CreatedAt: analyticsEvent.CreatedAt);

        return Result.Success(dto);
    }
}
