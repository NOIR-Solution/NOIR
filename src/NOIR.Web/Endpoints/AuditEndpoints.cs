using NOIR.Application.Common.Interfaces;
using NOIR.Application.Common.Models;
using NOIR.Application.Features.Audit.DTOs;
using NOIR.Application.Features.Audit.Queries.GetActivityDetails;
using NOIR.Application.Features.Audit.Queries.SearchActivityTimeline;
using NOIR.Domain.Common;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Activity Timeline API endpoints for audit logging.
/// </summary>
public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/audit")
            .WithTags("Audit")
            .RequireAuthorization();

        // Search activity timeline
        group.MapGet("/activity-timeline", async (
            string? pageContext,
            string? operationType,
            string? userId,
            string? targetId,
            string? correlationId,
            string? searchTerm,
            DateTimeOffset? fromDate,
            DateTimeOffset? toDate,
            bool? onlyFailed,
            int page,
            int pageSize,
            IMessageBus bus) =>
        {
            var query = new SearchActivityTimelineQuery(
                PageContext: pageContext,
                OperationType: operationType,
                UserId: userId,
                TargetId: targetId,
                CorrelationId: correlationId,
                SearchTerm: searchTerm,
                FromDate: fromDate,
                ToDate: toDate,
                OnlyFailed: onlyFailed,
                Page: page > 0 ? page : 1,
                PageSize: pageSize > 0 ? pageSize : 20);

            var result = await bus.InvokeAsync<Result<PagedResult<ActivityTimelineEntryDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AuditRead)
        .WithName("SearchActivityTimeline")
        .WithSummary("Search the activity timeline with filtering and pagination")
        .Produces<PagedResult<ActivityTimelineEntryDto>>(StatusCodes.Status200OK);

        // Get activity details
        group.MapGet("/activity-timeline/{id:guid}", async (
            Guid id,
            IMessageBus bus) =>
        {
            var query = new GetActivityDetailsQuery(id);
            var result = await bus.InvokeAsync<Result<ActivityDetailsDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AuditRead)
        .WithName("GetActivityDetails")
        .WithSummary("Get detailed information about a specific activity entry")
        .Produces<ActivityDetailsDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Get available page contexts (for filter dropdown)
        group.MapGet("/page-contexts", async (
            IAuditLogQueryService auditLogQueryService,
            CancellationToken ct) =>
        {
            var contexts = await auditLogQueryService.GetPageContextsAsync(ct);
            return Results.Ok(contexts);
        })
        .RequireAuthorization(Permissions.AuditRead)
        .WithName("GetPageContexts")
        .WithSummary("Get list of distinct page contexts for filtering")
        .Produces<IReadOnlyList<string>>(StatusCodes.Status200OK);
    }
}
