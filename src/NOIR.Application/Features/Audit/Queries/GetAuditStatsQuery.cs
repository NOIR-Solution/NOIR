namespace NOIR.Application.Features.Audit.Queries;

/// <summary>
/// Query to get current audit statistics for dashboard.
/// </summary>
public record GetAuditStatsQuery;

/// <summary>
/// Query to get detailed audit statistics for a date range.
/// </summary>
public record GetDetailedAuditStatsQuery(
    DateTimeOffset FromDate,
    DateTimeOffset ToDate);

/// <summary>
/// Handler for GetAuditStatsQuery.
/// </summary>
public class GetAuditStatsQueryHandler
{
    private readonly IAuditStatsService _statsService;
    private readonly ICurrentUser _currentUser;

    public GetAuditStatsQueryHandler(
        IAuditStatsService statsService,
        ICurrentUser currentUser)
    {
        _statsService = statsService;
        _currentUser = currentUser;
    }

    public async Task<Result<AuditStatsUpdate>> Handle(
        GetAuditStatsQuery query,
        CancellationToken ct)
    {
        var stats = await _statsService.GetCurrentStatsAsync(_currentUser.TenantId, ct);
        return Result.Success(stats);
    }
}

/// <summary>
/// Handler for GetDetailedAuditStatsQuery.
/// </summary>
public class GetDetailedAuditStatsQueryHandler
{
    private readonly IAuditStatsService _statsService;
    private readonly ICurrentUser _currentUser;

    public GetDetailedAuditStatsQueryHandler(
        IAuditStatsService statsService,
        ICurrentUser currentUser)
    {
        _statsService = statsService;
        _currentUser = currentUser;
    }

    public async Task<Result<AuditDetailedStats>> Handle(
        GetDetailedAuditStatsQuery query,
        CancellationToken ct)
    {
        var stats = await _statsService.GetDetailedStatsAsync(
            query.FromDate,
            query.ToDate,
            _currentUser.TenantId,
            ct);

        return Result.Success(stats);
    }
}
