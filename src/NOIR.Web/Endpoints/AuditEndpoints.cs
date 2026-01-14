using NOIR.Application.Common.Interfaces;
using NOIR.Application.Common.Models;
using NOIR.Application.Features.Audit.DTOs;
using NOIR.Application.Features.Audit.Queries;
using NOIR.Domain.Common;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Audit logging API endpoints.
/// Requires specific audit permissions for each endpoint.
/// </summary>
public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/audit")
            .WithTags("Audit")
            .RequireAuthorization()
            .CacheOutput("NoCache");

        group.MapGet("/trail/{correlationId}", async (string correlationId, IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<AuditTrailDto>>(new GetAuditTrailQuery(correlationId));
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AuditRead)
        .WithName("GetAuditTrail")
        .WithSummary("Get complete audit trail for a correlation ID")
        .Produces<AuditTrailDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        group.MapGet("/entity/{entityType}/{entityId}", async (
            string entityType,
            string entityId,
            int? pageNumber,
            int? pageSize,
            IMessageBus bus) =>
        {
            var query = new GetEntityHistoryQuery(
                entityType,
                entityId,
                pageNumber ?? 1,
                pageSize ?? 20);
            var result = await bus.InvokeAsync<Result<EntityHistoryDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AuditEntityHistory)
        .WithName("GetEntityHistory")
        .WithSummary("Get change history for a specific entity")
        .Produces<EntityHistoryDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        group.MapGet("/requests", async (
            int? pageNumber,
            int? pageSize,
            string? userId,
            string? httpMethod,
            int? statusCode,
            DateTimeOffset? fromDate,
            DateTimeOffset? toDate,
            IMessageBus bus) =>
        {
            var query = new GetHttpRequestAuditLogsQuery(
                pageNumber ?? 1,
                pageSize ?? 20,
                userId,
                httpMethod,
                statusCode,
                fromDate,
                toDate);
            var result = await bus.InvokeAsync<Result<PaginatedList<HttpRequestAuditDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AuditRead)
        .WithName("GetHttpRequestAuditLogs")
        .WithSummary("Get paginated HTTP request audit logs with filtering")
        .Produces<PaginatedList<HttpRequestAuditDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        group.MapGet("/handlers", async (
            int? pageNumber,
            int? pageSize,
            string? handlerName,
            string? operationType,
            bool? isSuccess,
            DateTimeOffset? fromDate,
            DateTimeOffset? toDate,
            IMessageBus bus) =>
        {
            var query = new GetHandlerAuditLogsQuery(
                pageNumber ?? 1,
                pageSize ?? 20,
                handlerName,
                operationType,
                isSuccess,
                fromDate,
                toDate);
            var result = await bus.InvokeAsync<Result<PaginatedList<HandlerAuditDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AuditRead)
        .WithName("GetHandlerAuditLogs")
        .WithSummary("Get paginated handler audit logs with filtering")
        .Produces<PaginatedList<HandlerAuditDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        group.MapGet("/export", async (
            DateTimeOffset? fromDate,
            DateTimeOffset? toDate,
            string? entityType,
            string? userId,
            string? format,
            int? maxRows,
            IMessageBus bus) =>
        {
            var exportFormat = format?.ToLowerInvariant() == "json"
                ? ExportFormat.Json
                : ExportFormat.Csv;

            var query = new ExportAuditLogsQuery(
                fromDate,
                toDate,
                entityType,
                userId,
                exportFormat,
                maxRows ?? 10000);
            var result = await bus.InvokeAsync<Result<ExportAuditLogsResult>>(query);

            if (result.IsFailure)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    detail: result.Error.Message);
            }

            return Results.File(
                result.Value.Data,
                result.Value.ContentType,
                result.Value.FileName);
        })
        .RequireAuthorization(Permissions.AuditExport)
        .RequireRateLimiting("export")
        .WithName("ExportAuditLogs")
        .WithSummary("Export audit logs to CSV or JSON for compliance reporting")
        .WithDescription("Maximum 100,000 rows per export. Date range limited to 90 days. Requires at least one filter.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
        .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests);
    }
}
