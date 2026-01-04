namespace NOIR.Application.Features.Audit.Queries;

/// <summary>
/// Wolverine handler for exporting audit logs to CSV/JSON for compliance reporting.
/// </summary>
public class ExportAuditLogsQueryHandler
{
    private readonly IAuditQueryService _auditQueryService;
    private readonly IDateTime _dateTime;

    public ExportAuditLogsQueryHandler(IAuditQueryService auditQueryService, IDateTime dateTime)
    {
        _auditQueryService = auditQueryService;
        _dateTime = dateTime;
    }

    public async Task<Result<ExportAuditLogsResult>> Handle(
        ExportAuditLogsQuery query,
        CancellationToken cancellationToken)
    {
        // VALIDATION: Enforce row limits to prevent memory exhaustion
        var maxRows = Math.Min(query.MaxRows, ExportAuditLogsQuery.MaxAllowedRows);
        if (maxRows <= 0)
        {
            return Result.Failure<ExportAuditLogsResult>(
                Error.Validation("MaxRows", "MaxRows must be greater than 0.", ErrorCodes.Validation.OutOfRange));
        }

        // VALIDATION: Enforce date range limits
        if (query.FromDate.HasValue && query.ToDate.HasValue)
        {
            var daysDiff = (query.ToDate.Value - query.FromDate.Value).TotalDays;
            if (daysDiff > ExportAuditLogsQuery.MaxDateRangeDays)
            {
                return Result.Failure<ExportAuditLogsResult>(
                    Error.Validation("DateRange",
                        $"Date range cannot exceed {ExportAuditLogsQuery.MaxDateRangeDays} days. Requested: {daysDiff:N0} days.", ErrorCodes.Validation.OutOfRange));
            }
            if (daysDiff < 0)
            {
                return Result.Failure<ExportAuditLogsResult>(
                    Error.Validation("DateRange", "FromDate must be before ToDate.", ErrorCodes.Validation.InvalidDate));
            }
        }

        // VALIDATION: Require at least one filter to prevent full table scan
        if (!query.FromDate.HasValue && !query.ToDate.HasValue &&
            string.IsNullOrWhiteSpace(query.EntityType) && string.IsNullOrWhiteSpace(query.UserId))
        {
            return Result.Failure<ExportAuditLogsResult>(
                Error.Validation("Filters", "At least one filter (FromDate, ToDate, EntityType, or UserId) is required.", ErrorCodes.Validation.Required));
        }

        // Get export data
        var exportData = await _auditQueryService.GetAuditExportDataAsync(
            maxRows,
            query.FromDate,
            query.ToDate,
            query.EntityType,
            query.UserId,
            cancellationToken);

        // Generate export
        byte[] data;
        string contentType;
        string fileName;

        if (query.Format == ExportFormat.Json)
        {
            data = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(exportData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
            contentType = "application/json";
            fileName = $"audit-export-{_dateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        }
        else
        {
            data = GenerateCsv(exportData);
            contentType = "text/csv";
            fileName = $"audit-export-{_dateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        }

        return Result.Success(new ExportAuditLogsResult(data, contentType, fileName));
    }

    private static byte[] GenerateCsv(IReadOnlyList<AuditExportData> rows)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Timestamp,CorrelationId,TenantId,UserId,UserEmail,IpAddress,EntityType,EntityId,Operation,HandlerName,Version,EntityDiff");

        // Data rows
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(row.Timestamp.ToString("o")),
                EscapeCsv(row.CorrelationId),
                EscapeCsv(row.TenantId),
                EscapeCsv(row.UserId),
                EscapeCsv(row.UserEmail),
                EscapeCsv(row.IpAddress),
                EscapeCsv(row.EntityType),
                EscapeCsv(row.EntityId),
                EscapeCsv(row.Operation),
                EscapeCsv(row.HandlerName),
                row.Version.ToString(),
                EscapeCsv(row.EntityDiff)));
        }

        // Include UTF-8 BOM for proper encoding detection in Excel and other applications
        var content = Encoding.UTF8.GetBytes(sb.ToString());
        var bom = Encoding.UTF8.GetPreamble();
        var result = new byte[bom.Length + content.Length];
        bom.CopyTo(result, 0);
        content.CopyTo(result, bom.Length);
        return result;
    }

    /// <summary>
    /// Escapes a value for safe CSV output.
    /// Prevents CSV injection attacks (formula execution in Excel).
    /// </summary>
    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";

        // CRITICAL: Prevent CSV injection (formula execution in Excel/Sheets)
        // Values starting with =, +, -, @, tab, or carriage return can execute formulas
        // Always wrap in quotes and escape internal quotes to prevent formula injection
        var needsPrefix = value.Length > 0 && (
            value[0] == '=' || value[0] == '+' ||
            value[0] == '-' || value[0] == '@' ||
            value[0] == '\t' || value[0] == '\r' ||
            value[0] == '|' || value[0] == '%');

        // Always quote values to ensure safe handling
        // Escape double quotes by doubling them and prefix with single quote if formula-like
        var escaped = value.Replace("\"", "\"\"");
        if (needsPrefix)
        {
            return $"\"'{escaped}\""; // Wrap in quotes with single quote prefix
        }
        return $"\"{escaped}\"";
    }
}
