using Microsoft.AspNetCore.Mvc;
using NOIR.Application.Common.Interfaces;
using NOIR.Application.Features.DeveloperLogs.DTOs;
using NOIR.Domain.Common;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Developer Log API endpoints for log viewing, level control, and historical access.
/// </summary>
public static class DeveloperLogEndpoints
{
    public static void MapDeveloperLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/developer-logs")
            .WithTags("Developer Logs")
            .RequireAuthorization(Permissions.SystemAdmin);

        // ========== Log Level Control ==========

        // Get current log level
        group.MapGet("/level", (ILogLevelService logLevelService) =>
        {
            var level = logLevelService.GetCurrentLevel();
            var availableLevels = logLevelService.GetAvailableLevels();
            return Results.Ok(new LogLevelResponse(level, availableLevels));
        })
        .WithName("GetLogLevel")
        .WithSummary("Get the current global minimum log level")
        .Produces<LogLevelResponse>(StatusCodes.Status200OK);

        // Set log level
        group.MapPut("/level", (
            ChangeLogLevelRequest request,
            ILogLevelService logLevelService,
            ILogStreamHubContext hubContext) =>
        {
            var success = logLevelService.SetLevel(request.Level);

            if (!success)
            {
                return Results.BadRequest(new { Error = "Invalid log level. Valid levels: Verbose, Debug, Information, Warning, Error, Fatal" });
            }

            // Notify all connected clients
            _ = hubContext.NotifyLevelChangedAsync(request.Level);

            var availableLevels = logLevelService.GetAvailableLevels();
            return Results.Ok(new LogLevelResponse(request.Level, availableLevels));
        })
        .WithName("SetLogLevel")
        .WithSummary("Set the global minimum log level dynamically (no restart required)")
        .Produces<LogLevelResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // Get log level overrides
        group.MapGet("/level/overrides", (ILogLevelService logLevelService) =>
        {
            var globalLevel = logLevelService.GetCurrentLevel();
            var overrides = logLevelService.GetOverrides();
            return Results.Ok(new LogLevelOverridesResponse(globalLevel, overrides));
        })
        .WithName("GetLogLevelOverrides")
        .WithSummary("Get all source-specific log level overrides")
        .Produces<LogLevelOverridesResponse>(StatusCodes.Status200OK);

        // Set log level override for a source
        group.MapPut("/level/overrides/{sourcePrefix}", (
            string sourcePrefix,
            ChangeLogLevelRequest request,
            ILogLevelService logLevelService) =>
        {
            var success = logLevelService.SetOverride(sourcePrefix, request.Level);

            if (!success)
            {
                return Results.BadRequest(new { Error = "Invalid log level or source prefix" });
            }

            return Results.Ok(new { Source = sourcePrefix, Level = request.Level });
        })
        .WithName("SetLogLevelOverride")
        .WithSummary("Set log level override for a specific source namespace")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // Remove log level override
        group.MapDelete("/level/overrides/{sourcePrefix}", (
            string sourcePrefix,
            ILogLevelService logLevelService) =>
        {
            var success = logLevelService.RemoveOverride(sourcePrefix);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoveLogLevelOverride")
        .WithSummary("Remove a source-specific log level override")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // ========== Live Buffer ==========

        // Get buffer statistics
        group.MapGet("/buffer/stats", (ILogRingBuffer buffer) =>
        {
            var stats = buffer.GetStats();
            return Results.Ok(stats);
        })
        .WithName("GetLogBufferStats")
        .WithSummary("Get statistics about the in-memory log buffer")
        .Produces<LogBufferStatsDto>(StatusCodes.Status200OK);

        // Get recent log entries from buffer
        group.MapGet("/buffer/entries", (
            [FromQuery] int count,
            [FromQuery] string? minLevel,
            [FromQuery] string? sources,
            [FromQuery] string? search,
            [FromQuery] bool? exceptionsOnly,
            ILogRingBuffer buffer) =>
        {
            var actualCount = Math.Min(Math.Max(count, 10), 1000);
            var sourcesArray = string.IsNullOrEmpty(sources) ? null : sources.Split(',');

            // Parse min level from string
            DevLogLevel? parsedMinLevel = null;
            if (!string.IsNullOrEmpty(minLevel) && Enum.TryParse<DevLogLevel>(minLevel, true, out var level))
            {
                parsedMinLevel = level;
            }

            var entries = buffer.GetFiltered(
                parsedMinLevel,
                sourcesArray,
                search,
                exceptionsOnly ?? false,
                actualCount);

            return Results.Ok(entries);
        })
        .WithName("GetBufferEntries")
        .WithSummary("Get filtered log entries from the in-memory buffer")
        .Produces<IEnumerable<LogEntryDto>>(StatusCodes.Status200OK);

        // Get error clusters
        group.MapGet("/buffer/errors", (
            [FromQuery] int maxClusters,
            ILogRingBuffer buffer) =>
        {
            var actualMax = Math.Min(Math.Max(maxClusters, 5), 50);
            var clusters = buffer.GetErrorClusters(actualMax);
            return Results.Ok(clusters);
        })
        .WithName("GetErrorClusters")
        .WithSummary("Get error patterns grouped by similarity")
        .Produces<IEnumerable<ErrorClusterDto>>(StatusCodes.Status200OK);

        // Clear buffer
        group.MapDelete("/buffer", (
            ILogRingBuffer buffer,
            ILogStreamHubContext hubContext,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("DeveloperLogEndpoints");
            buffer.Clear();

            // Notify connected clients
            var stats = buffer.GetStats();
            _ = hubContext.SendBufferStatsAsync(stats);

            logger.LogWarning("Log buffer cleared via API");
            return Results.NoContent();
        })
        .WithName("ClearLogBuffer")
        .WithSummary("Clear all entries from the in-memory log buffer")
        .Produces(StatusCodes.Status204NoContent);

        // ========== Historical Logs ==========

        // Get available log dates
        group.MapGet("/history/dates", async (
            IHistoricalLogService historicalLogService,
            CancellationToken ct) =>
        {
            var dates = await historicalLogService.GetAvailableDatesAsync(ct);
            return Results.Ok(dates.Select(d => d.ToString("yyyy-MM-dd")));
        })
        .WithName("GetAvailableLogDates")
        .WithSummary("Get list of dates that have log files available")
        .Produces<IEnumerable<string>>(StatusCodes.Status200OK);

        // Get logs for a specific date
        group.MapGet("/history/{date}", async (
            string date,
            [FromQuery] string? search,
            [FromQuery] string? minLevel,
            [FromQuery] string? levels,
            [FromQuery] string? sources,
            [FromQuery] bool? hasException,
            [FromQuery] string? requestId,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IHistoricalLogService historicalLogService,
            CancellationToken ct) =>
        {
            if (!DateOnly.TryParse(date, out var parsedDate))
            {
                return Results.BadRequest(new { Error = "Invalid date format. Use yyyy-MM-dd" });
            }

            // Parse min level from string
            DevLogLevel? parsedMinLevel = null;
            if (!string.IsNullOrEmpty(minLevel) && Enum.TryParse<DevLogLevel>(minLevel, true, out var level))
            {
                parsedMinLevel = level;
            }

            // Parse levels array from comma-separated string
            DevLogLevel[]? parsedLevels = null;
            if (!string.IsNullOrEmpty(levels))
            {
                parsedLevels = levels.Split(',')
                    .Select(l => Enum.TryParse<DevLogLevel>(l.Trim(), true, out var lvl) ? lvl : (DevLogLevel?)null)
                    .Where(l => l.HasValue)
                    .Select(l => l!.Value)
                    .ToArray();
            }

            var query = new LogSearchQuery
            {
                Search = search,
                MinLevel = parsedMinLevel,
                Levels = parsedLevels,
                Sources = string.IsNullOrEmpty(sources) ? null : sources.Split(','),
                HasException = hasException,
                RequestId = requestId,
                Page = page > 0 ? page : 1,
                PageSize = pageSize > 0 ? Math.Min(pageSize, 500) : 100
            };

            var result = await historicalLogService.GetLogsAsync(parsedDate, query, ct);
            return Results.Ok(result);
        })
        .WithName("GetHistoricalLogs")
        .WithSummary("Get paginated log entries for a specific date")
        .Produces<LogEntriesPagedResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // Search logs across date range
        group.MapGet("/history/search", async (
            [FromQuery] string fromDate,
            [FromQuery] string toDate,
            [FromQuery] string? search,
            [FromQuery] string? minLevel,
            [FromQuery] string? levels,
            [FromQuery] string? sources,
            [FromQuery] bool? hasException,
            [FromQuery] string? requestId,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IHistoricalLogService historicalLogService,
            CancellationToken ct) =>
        {
            if (!DateOnly.TryParse(fromDate, out var from))
            {
                return Results.BadRequest(new { Error = "Invalid fromDate format. Use yyyy-MM-dd" });
            }

            if (!DateOnly.TryParse(toDate, out var to))
            {
                return Results.BadRequest(new { Error = "Invalid toDate format. Use yyyy-MM-dd" });
            }

            // Limit date range to prevent excessive scanning
            if ((to.DayNumber - from.DayNumber) > 30)
            {
                return Results.BadRequest(new { Error = "Date range cannot exceed 30 days" });
            }

            // Parse min level from string
            DevLogLevel? parsedMinLevel = null;
            if (!string.IsNullOrEmpty(minLevel) && Enum.TryParse<DevLogLevel>(minLevel, true, out var level))
            {
                parsedMinLevel = level;
            }

            // Parse levels array from comma-separated string
            DevLogLevel[]? parsedLevels = null;
            if (!string.IsNullOrEmpty(levels))
            {
                parsedLevels = levels.Split(',')
                    .Select(l => Enum.TryParse<DevLogLevel>(l.Trim(), true, out var lvl) ? lvl : (DevLogLevel?)null)
                    .Where(l => l.HasValue)
                    .Select(l => l!.Value)
                    .ToArray();
            }

            var query = new LogSearchQuery
            {
                Search = search,
                MinLevel = parsedMinLevel,
                Levels = parsedLevels,
                Sources = string.IsNullOrEmpty(sources) ? null : sources.Split(','),
                HasException = hasException,
                RequestId = requestId,
                Page = page > 0 ? page : 1,
                PageSize = pageSize > 0 ? Math.Min(pageSize, 500) : 100
            };

            var result = await historicalLogService.SearchLogsAsync(from, to, query, ct);
            return Results.Ok(result);
        })
        .WithName("SearchHistoricalLogs")
        .WithSummary("Search log entries across a date range (max 30 days)")
        .Produces<LogEntriesPagedResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // Get log file size for date range
        group.MapGet("/history/size", async (
            [FromQuery] string fromDate,
            [FromQuery] string toDate,
            IHistoricalLogService historicalLogService,
            CancellationToken ct) =>
        {
            if (!DateOnly.TryParse(fromDate, out var from) || !DateOnly.TryParse(toDate, out var to))
            {
                return Results.BadRequest(new { Error = "Invalid date format. Use yyyy-MM-dd" });
            }

            var size = await historicalLogService.GetLogFileSizeAsync(from, to, ct);
            return Results.Ok(new { SizeBytes = size, SizeFormatted = FormatBytes(size) });
        })
        .WithName("GetHistoricalLogSize")
        .WithSummary("Get total file size of logs for a date range")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        var order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
