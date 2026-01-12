using NOIR.Application.Common.Interfaces;
using NOIR.Application.Common.Models;
using NOIR.Application.Features.Audit.Commands.CreateRetentionPolicy;
using NOIR.Application.Features.Audit.Commands.DeleteRetentionPolicy;
using NOIR.Application.Features.Audit.Commands.UpdateRetentionPolicy;
using NOIR.Application.Features.Audit.DTOs;
using NOIR.Application.Features.Audit.Queries;
using NOIR.Domain.Common;
using NOIR.Infrastructure.Audit;

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
            .RequireAuthorization() // Base authorization - specific permissions per endpoint
            .CacheOutput("NoCache");

        // Get complete audit trail by correlation ID
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

        // Get entity change history
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

        // Get paginated HTTP request audit logs
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

        // Get paginated handler audit logs
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

        // Full-text search across all audit logs
        group.MapGet("/search", async (
            string query,
            string? entityType,
            DateTimeOffset? fromDate,
            DateTimeOffset? toDate,
            int? pageNumber,
            int? pageSize,
            IMessageBus bus) =>
        {
            var searchQuery = new SearchAuditLogsQuery(
                query,
                pageNumber ?? 1,
                pageSize ?? 50,
                fromDate,
                toDate,
                AuditSearchScope.All,
                entityType);
            var result = await bus.InvokeAsync<Result<AuditSearchResult>>(searchQuery);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AuditRead)
        .WithName("SearchAuditLogs")
        .WithSummary("Full-text search across all audit logs")
        .Produces<AuditSearchResult>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        // Get current audit statistics for dashboard
        group.MapGet("/stats", async (IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<AuditStatsUpdate>>(new GetAuditStatsQuery());
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AuditRead)
        .WithName("GetAuditStats")
        .WithSummary("Get current audit statistics for dashboard")
        .Produces<AuditStatsUpdate>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        // Get detailed audit statistics for date range
        group.MapGet("/stats/detailed", async (
            DateTimeOffset fromDate,
            DateTimeOffset toDate,
            IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<AuditDetailedStats>>(
                new GetDetailedAuditStatsQuery(fromDate, toDate));
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AuditRead)
        .WithName("GetDetailedAuditStats")
        .WithSummary("Get detailed audit statistics for a date range")
        .Produces<AuditDetailedStats>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        // Retention Policy CRUD endpoints
        var policiesGroup = group.MapGroup("/policies")
            .WithTags("Audit Retention Policies");

        // List all retention policies
        policiesGroup.MapGet("/", async (IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<List<AuditRetentionPolicyDto>>>(
                new GetRetentionPoliciesQuery());
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AuditPolicyRead)
        .WithName("GetRetentionPolicies")
        .WithSummary("List all audit retention policies")
        .Produces<List<AuditRetentionPolicyDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        // Get single retention policy
        policiesGroup.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<AuditRetentionPolicyDto>>(
                new GetRetentionPolicyByIdQuery(id));
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AuditPolicyRead)
        .WithName("GetRetentionPolicyById")
        .WithSummary("Get a specific audit retention policy")
        .Produces<AuditRetentionPolicyDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        // Get available compliance presets
        policiesGroup.MapGet("/presets", () =>
        {
            var presets = new List<CompliancePresetDto>
            {
                new("GDPR", "General Data Protection Regulation", 30, 60, 275, 365,
                    "EU data protection - 1 year retention"),
                new("SOX", "Sarbanes-Oxley Act", 90, 365, 1825, 2555,
                    "Financial records - 7 year retention"),
                new("HIPAA", "Health Insurance Portability Act", 60, 180, 1460, 2190,
                    "Healthcare records - 6 year retention"),
                new("PCI-DSS", "Payment Card Industry Data Security Standard", 30, 90, 275, 365,
                    "Payment data - 1 year retention"),
                new("Custom", "Custom retention settings", 30, 90, 365, 2555,
                    "Configure your own retention periods")
            };
            return Results.Ok(presets);
        })
        .RequireAuthorization(Permissions.AuditPolicyRead)
        .WithName("GetCompliancePresets")
        .WithSummary("Get available compliance presets for retention policies")
        .Produces<List<CompliancePresetDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        // Create retention policy
        policiesGroup.MapPost("/", async (CreateRetentionPolicyCommand command, IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<AuditRetentionPolicyDto>>(command);
            return result.IsSuccess
                ? Results.Created($"/api/audit/policies/{result.Value.Id}", result.Value)
                : result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AuditPolicyWrite)
        .WithName("CreateRetentionPolicy")
        .WithSummary("Create a new audit retention policy")
        .Produces<AuditRetentionPolicyDto>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        // Update retention policy
        policiesGroup.MapPut("/{id:guid}", async (Guid id, UpdateRetentionPolicyCommand command, IMessageBus bus) =>
        {
            if (id != command.Id)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    detail: "Route ID does not match command ID");
            }
            var result = await bus.InvokeAsync<Result<AuditRetentionPolicyDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AuditPolicyWrite)
        .WithName("UpdateRetentionPolicy")
        .WithSummary("Update an existing audit retention policy")
        .Produces<AuditRetentionPolicyDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        // Delete retention policy
        policiesGroup.MapDelete("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result>(new DeleteRetentionPolicyCommand(id));
            return result.IsSuccess
                ? Results.NoContent()
                : result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.AuditPolicyDelete)
        .WithName("DeleteRetentionPolicy")
        .WithSummary("Delete an audit retention policy")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        // Export audit logs (CSV/JSON) for compliance
        // Rate limited to prevent abuse (expensive operation)
        // Requires AuditExport permission (more sensitive than reading)
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
        .RequireRateLimiting("export") // Stricter rate limit for expensive exports
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
