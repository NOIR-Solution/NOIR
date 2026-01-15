namespace NOIR.Application.Features.Audit.Queries.SearchActivityTimeline;

/// <summary>
/// Query to search the activity timeline with filtering and pagination.
/// </summary>
public sealed record SearchActivityTimelineQuery(
    /// <summary>
    /// Filter by page context (e.g., "Users", "Tenants").
    /// </summary>
    string? PageContext = null,

    /// <summary>
    /// Filter by operation type (Create, Update, Delete).
    /// </summary>
    string? OperationType = null,

    /// <summary>
    /// Filter by user ID.
    /// </summary>
    string? UserId = null,

    /// <summary>
    /// Filter by target entity ID (e.g., user ID, tenant ID).
    /// </summary>
    string? TargetId = null,

    /// <summary>
    /// Filter by correlation ID (same HTTP request).
    /// </summary>
    string? CorrelationId = null,

    /// <summary>
    /// Search term to filter by display name or description.
    /// </summary>
    string? SearchTerm = null,

    /// <summary>
    /// Filter from date.
    /// </summary>
    DateTimeOffset? FromDate = null,

    /// <summary>
    /// Filter to date.
    /// </summary>
    DateTimeOffset? ToDate = null,

    /// <summary>
    /// Only show failed operations.
    /// </summary>
    bool? OnlyFailed = null,

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    int Page = 1,

    /// <summary>
    /// Page size.
    /// </summary>
    int PageSize = 20);
