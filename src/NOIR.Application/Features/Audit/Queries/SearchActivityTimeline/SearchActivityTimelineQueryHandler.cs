using NOIR.Application.Common.Interfaces;
using NOIR.Application.Common.Models;
using NOIR.Application.Features.Audit.DTOs;

namespace NOIR.Application.Features.Audit.Queries.SearchActivityTimeline;

/// <summary>
/// Handler for searching the activity timeline.
/// </summary>
public sealed class SearchActivityTimelineQueryHandler
{
    private readonly IAuditLogQueryService _auditLogQueryService;

    public SearchActivityTimelineQueryHandler(IAuditLogQueryService auditLogQueryService)
    {
        _auditLogQueryService = auditLogQueryService;
    }

    public async Task<Result<PagedResult<ActivityTimelineEntryDto>>> Handle(
        SearchActivityTimelineQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _auditLogQueryService.SearchActivityTimelineAsync(
            query.PageContext,
            query.OperationType,
            query.UserId,
            query.TargetId,
            query.CorrelationId,
            query.SearchTerm,
            query.FromDate,
            query.ToDate,
            query.OnlyFailed,
            query.Page,
            query.PageSize,
            cancellationToken);

        return Result.Success(result);
    }
}
