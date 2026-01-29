namespace NOIR.Application.Features.FilterAnalytics.Commands.CreateFilterEvent;

/// <summary>
/// Command to create a filter analytics event.
/// Used to track filter usage for analytics purposes.
/// </summary>
public sealed record CreateFilterEventCommand(
    string SessionId,
    FilterEventType EventType,
    int ProductCount,
    string? CategorySlug = null,
    string? FilterCode = null,
    string? FilterValue = null,
    string? SearchQuery = null,
    Guid? ClickedProductId = null);
