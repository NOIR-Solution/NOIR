namespace NOIR.Infrastructure.Logging;

/// <summary>
/// Service for accessing historical log files.
/// </summary>
public sealed class HistoricalLogService : IHistoricalLogService, IScopedService
{
    private readonly DeveloperLogSettings _settings;
    private readonly ILogger<HistoricalLogService> _logger;
    private readonly string _logDirectory;
    private readonly Regex _logFilePattern;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public HistoricalLogService(
        IOptions<DeveloperLogSettings> options,
        ILogger<HistoricalLogService> logger,
        IWebHostEnvironment environment)
    {
        _settings = options.Value;
        _logger = logger;

        // Extract directory from log file path pattern
        var logPath = _settings.LogFilePath;
        _logDirectory = Path.GetDirectoryName(
            Path.Combine(environment.ContentRootPath, logPath)) ?? "logs";

        // Pattern for log files: noir-20260116.json or noir-20260116.json.gz
        _logFilePattern = new Regex(@"noir-(\d{8})\.json(\.gz)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    /// <inheritdoc />
    public Task<IEnumerable<DateOnly>> GetAvailableDatesAsync(CancellationToken ct = default)
    {
        var dates = new List<DateOnly>();

        if (!Directory.Exists(_logDirectory))
        {
            return Task.FromResult<IEnumerable<DateOnly>>(dates);
        }

        foreach (var file in Directory.GetFiles(_logDirectory, "noir-*.json*"))
        {
            var fileName = Path.GetFileName(file);
            var match = _logFilePattern.Match(fileName);

            if (match.Success && DateTime.TryParseExact(
                match.Groups[1].Value,
                "yyyyMMdd",
                null,
                System.Globalization.DateTimeStyles.None,
                out var date))
            {
                dates.Add(DateOnly.FromDateTime(date));
            }
        }

        return Task.FromResult<IEnumerable<DateOnly>>(dates.OrderByDescending(d => d));
    }

    /// <inheritdoc />
    public async Task<LogEntriesPagedResponse> GetLogsAsync(
        DateOnly date,
        LogSearchQuery query,
        CancellationToken ct = default)
    {
        var logFile = GetLogFilePath(date);

        if (!File.Exists(logFile) && !File.Exists(logFile + ".gz"))
        {
            return new LogEntriesPagedResponse
            {
                Items = [],
                TotalCount = 0,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalPages = 0
            };
        }

        var actualFile = File.Exists(logFile) ? logFile : logFile + ".gz";
        var entries = await ReadLogFileAsync(actualFile, query, ct);

        return CreatePagedResponse(entries, query);
    }

    /// <inheritdoc />
    public async Task<LogEntriesPagedResponse> SearchLogsAsync(
        DateOnly fromDate,
        DateOnly toDate,
        LogSearchQuery query,
        CancellationToken ct = default)
    {
        var allEntries = new List<LogEntryDto>();

        // Get all dates in range
        var dates = Enumerable.Range(0, toDate.DayNumber - fromDate.DayNumber + 1)
            .Select(d => fromDate.AddDays(d))
            .OrderByDescending(d => d)
            .ToList();

        foreach (var date in dates)
        {
            if (ct.IsCancellationRequested) break;

            var logFile = GetLogFilePath(date);
            var actualFile = File.Exists(logFile) ? logFile :
                             File.Exists(logFile + ".gz") ? logFile + ".gz" : null;

            if (actualFile == null) continue;

            var entries = await ReadLogFileAsync(actualFile, query with { Page = 1, PageSize = int.MaxValue }, ct);
            allEntries.AddRange(entries);

            // Early termination if we have enough entries
            if (allEntries.Count >= query.Page * query.PageSize + query.PageSize)
            {
                break;
            }
        }

        // Sort by timestamp descending
        allEntries = allEntries.OrderByDescending(e => e.Timestamp).ToList();

        return CreatePagedResponse(allEntries, query);
    }

    /// <inheritdoc />
    public Task<long> GetLogFileSizeAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        long totalSize = 0;

        if (!Directory.Exists(_logDirectory))
        {
            return Task.FromResult(0L);
        }

        var dates = Enumerable.Range(0, toDate.DayNumber - fromDate.DayNumber + 1)
            .Select(d => fromDate.AddDays(d))
            .ToList();

        foreach (var date in dates)
        {
            var logFile = GetLogFilePath(date);

            if (File.Exists(logFile))
            {
                totalSize += new FileInfo(logFile).Length;
            }
            else if (File.Exists(logFile + ".gz"))
            {
                totalSize += new FileInfo(logFile + ".gz").Length;
            }
        }

        return Task.FromResult(totalSize);
    }

    private string GetLogFilePath(DateOnly date)
    {
        return Path.Combine(_logDirectory, $"noir-{date:yyyyMMdd}.json");
    }

    private async Task<List<LogEntryDto>> ReadLogFileAsync(
        string filePath,
        LogSearchQuery query,
        CancellationToken ct)
    {
        var entries = new List<LogEntryDto>();
        var searchRegex = CreateSearchRegex(query.Search);

        try
        {
            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Stream readStream = fileStream;

            // Handle gzipped files
            if (filePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            {
                readStream = new GZipStream(fileStream, CompressionMode.Decompress);
            }

            using var reader = new StreamReader(readStream);
            long lineNumber = 0;

            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null && !ct.IsCancellationRequested)
            {
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var entry = ParseLogLine(line, lineNumber);
                    if (entry != null && MatchesQuery(entry, query, searchRegex))
                    {
                        entries.Add(entry);
                    }
                }
                catch (JsonException)
                {
                    // Skip malformed lines
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading log file {FilePath}", filePath);
        }

        return entries;
    }

    private static LogEntryDto? ParseLogLine(string line, long lineNumber)
    {
        try
        {
            // Serilog JSON format - supports both compact (@l, @t, @m) and full (Level, Timestamp, MessageTemplate) formats
            var json = JsonDocument.Parse(line);
            var root = json.RootElement;

            // Level: check @l (compact) then Level (full)
            DevLogLevel level;
            if (root.TryGetProperty("@l", out var levelPropCompact))
            {
                level = ParseLevel(levelPropCompact.GetString());
            }
            else if (root.TryGetProperty("Level", out var levelPropFull))
            {
                level = ParseLevel(levelPropFull.GetString());
            }
            else
            {
                level = DevLogLevel.Information;
            }

            // Timestamp: check @t (compact) then Timestamp (full)
            DateTimeOffset timestamp;
            if (root.TryGetProperty("@t", out var timePropCompact))
            {
                timestamp = DateTimeOffset.Parse(timePropCompact.GetString()!);
            }
            else if (root.TryGetProperty("Timestamp", out var timePropFull))
            {
                timestamp = DateTimeOffset.Parse(timePropFull.GetString()!);
            }
            else
            {
                timestamp = DateTimeOffset.UtcNow;
            }

            // Message: check @m (compact rendered), then RenderedMessage (full), then render template from @mt or MessageTemplate
            var message = "";
            var hasRenderedMessage = false;
            if (root.TryGetProperty("@m", out var msgProp))
            {
                message = msgProp.GetString() ?? "";
                hasRenderedMessage = true;
            }
            else if (root.TryGetProperty("RenderedMessage", out var renderedProp))
            {
                message = renderedProp.GetString() ?? "";
                hasRenderedMessage = true;
            }

            // If no rendered message, render the template by substituting property values
            if (!hasRenderedMessage)
            {
                string? template = null;
                if (root.TryGetProperty("@mt", out var mtProp))
                {
                    template = mtProp.GetString();
                }
                else if (root.TryGetProperty("MessageTemplate", out var templateMsgProp))
                {
                    template = templateMsgProp.GetString();
                }

                if (!string.IsNullOrEmpty(template))
                {
                    message = RenderMessageTemplate(template, root);
                }
            }

            // MessageTemplate: check @mt (compact) then MessageTemplate (full)
            string? messageTemplate = null;
            if (root.TryGetProperty("@mt", out var templateProp))
            {
                messageTemplate = templateProp.GetString();
            }
            else if (root.TryGetProperty("MessageTemplate", out var templatePropFull))
            {
                messageTemplate = templatePropFull.GetString();
            }

            // SourceContext: check in Properties object (full format) or root (compact)
            string? sourceContext = null;
            if (root.TryGetProperty("SourceContext", out var srcProp))
            {
                sourceContext = srcProp.GetString();
            }
            else if (root.TryGetProperty("Properties", out var propsProp) &&
                     propsProp.TryGetProperty("SourceContext", out var nestedSrcProp))
            {
                sourceContext = nestedSrcProp.GetString();
            }

            // Exception: check @x (compact) then Exception (full)
            ExceptionDto? exception = null;
            if (root.TryGetProperty("@x", out var exProp))
            {
                var exceptionText = exProp.GetString();
                if (!string.IsNullOrEmpty(exceptionText))
                {
                    exception = ParseExceptionFromText(exceptionText);
                }
            }
            else if (root.TryGetProperty("Exception", out var exPropFull))
            {
                var exceptionText = exPropFull.GetString();
                if (!string.IsNullOrEmpty(exceptionText))
                {
                    exception = ParseExceptionFromText(exceptionText);
                }
            }

            // Extract other properties - handle both compact (root level) and full format (nested Properties object)
            var properties = new Dictionary<string, object?>();
            var excludedProps = new HashSet<string>
            {
                "@t", "@l", "@m", "@mt", "@x", "@r", "@i",  // Compact format special props
                "Timestamp", "Level", "MessageTemplate", "RenderedMessage", "Exception", "Properties",  // Full format special props
                "SourceContext", "RequestId", "TraceId", "UserId", "TenantId", "MachineName", "Environment"  // Common metadata
            };

            // Check for nested Properties object (full format)
            if (root.TryGetProperty("Properties", out var propsObj) && propsObj.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in propsObj.EnumerateObject())
                {
                    if (!excludedProps.Contains(prop.Name))
                    {
                        properties[prop.Name] = GetJsonValue(prop.Value);
                    }
                }
            }
            else
            {
                // Compact format - properties at root level
                foreach (var prop in root.EnumerateObject())
                {
                    if (!excludedProps.Contains(prop.Name))
                    {
                        properties[prop.Name] = GetJsonValue(prop.Value);
                    }
                }
            }

            // Extract special fields - check both root and nested Properties
            string? GetSpecialField(string name)
            {
                if (root.TryGetProperty(name, out var prop))
                    return prop.GetString();
                if (root.TryGetProperty("Properties", out var propsElement) &&
                    propsElement.TryGetProperty(name, out var nestedProp))
                    return nestedProp.GetString();
                return null;
            }

            return new LogEntryDto
            {
                Id = lineNumber,
                Timestamp = timestamp,
                Level = level,
                Message = message,
                MessageTemplate = messageTemplate,
                SourceContext = sourceContext,
                Exception = exception,
                Properties = properties.Count > 0 ? properties : null,
                RequestId = GetSpecialField("RequestId"),
                TraceId = GetSpecialField("TraceId"),
                UserId = GetSpecialField("UserId"),
                TenantId = GetSpecialField("TenantId")
            };
        }
        catch
        {
            return null;
        }
    }

    private static DevLogLevel ParseLevel(string? level)
    {
        return level?.ToUpperInvariant() switch
        {
            "VERBOSE" or "VRB" => DevLogLevel.Verbose,
            "DEBUG" or "DBG" => DevLogLevel.Debug,
            "INFORMATION" or "INF" or "INFO" => DevLogLevel.Information,
            "WARNING" or "WRN" or "WARN" => DevLogLevel.Warning,
            "ERROR" or "ERR" => DevLogLevel.Error,
            "FATAL" or "FTL" => DevLogLevel.Fatal,
            _ => DevLogLevel.Information
        };
    }

    private static ExceptionDto? ParseExceptionFromText(string text)
    {
        // Parse exception from formatted text (stack trace format)
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return null;

        // First line usually contains type and message
        var firstLine = lines[0];
        var colonIndex = firstLine.IndexOf(':');

        var type = colonIndex > 0 ? firstLine[..colonIndex].Trim() : "Exception";
        var message = colonIndex > 0 && colonIndex < firstLine.Length - 1
            ? firstLine[(colonIndex + 1)..].Trim()
            : firstLine;

        var stackTrace = lines.Length > 1
            ? string.Join("\n", lines.Skip(1))
            : null;

        return new ExceptionDto
        {
            Type = type,
            Message = message,
            StackTrace = stackTrace
        };
    }

    private static object? GetJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(GetJsonValue).ToArray(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => GetJsonValue(p.Value)),
            _ => element.GetRawText()
        };
    }

    /// <summary>
    /// Renders a Serilog message template by substituting {PropertyName} placeholders with actual values.
    /// </summary>
    private static string RenderMessageTemplate(string template, JsonElement root)
    {
        if (string.IsNullOrEmpty(template)) return template;

        // Get the Properties object if it exists
        JsonElement? propsElement = null;
        if (root.TryGetProperty("Properties", out var props) && props.ValueKind == JsonValueKind.Object)
        {
            propsElement = props;
        }

        // Replace {PropertyName} patterns with their values
        // Serilog uses {Name} or {@Name} (destructured) or {$Name} (stringified) or {Name:format}
        return Regex.Replace(template, @"\{[@$]?(\w+)(?::[^}]*)?\}", match =>
        {
            var propName = match.Groups[1].Value;

            // First check in Properties object
            if (propsElement.HasValue && propsElement.Value.TryGetProperty(propName, out var propValue))
            {
                return FormatPropertyValue(propValue);
            }

            // Then check at root level (for compact format)
            if (root.TryGetProperty(propName, out var rootPropValue))
            {
                return FormatPropertyValue(rootPropValue);
            }

            // Return original placeholder if not found
            return match.Value;
        });
    }

    /// <summary>
    /// Formats a JSON property value for display in the rendered message.
    /// </summary>
    private static string FormatPropertyValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "True",
            JsonValueKind.False => "False",
            JsonValueKind.Null => "null",
            JsonValueKind.Array => $"[{string.Join(", ", element.EnumerateArray().Select(FormatPropertyValue))}]",
            JsonValueKind.Object => FormatObjectValue(element),
            _ => element.GetRawText()
        };
    }

    /// <summary>
    /// Formats a JSON object for display, keeping it concise.
    /// </summary>
    private static string FormatObjectValue(JsonElement element)
    {
        // For simple objects with a few properties, show them inline
        var props = element.EnumerateObject().ToList();
        if (props.Count == 0) return "{}";
        if (props.Count <= 3)
        {
            var items = props.Select(p => $"{p.Name}: {FormatPropertyValue(p.Value)}");
            return $"{{ {string.Join(", ", items)} }}";
        }
        // For complex objects, just show property count
        return $"{{...{props.Count} properties}}";
    }

    private static Regex? CreateSearchRegex(string? search)
    {
        if (string.IsNullOrEmpty(search)) return null;

        try
        {
            // If starts with /, treat as regex
            if (search.StartsWith('/') && search.EndsWith('/') && search.Length > 2)
            {
                return new Regex(search[1..^1], RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }

            // Otherwise, treat as literal with wildcards
            var pattern = Regex.Escape(search)
                .Replace("\\*", ".*")
                .Replace("\\?", ".");

            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
        catch
        {
            // If regex creation fails, use literal search
            return new Regex(Regex.Escape(search), RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }

    private static bool MatchesQuery(LogEntryDto entry, LogSearchQuery query, Regex? searchRegex)
    {
        // Level filter
        if (query.MinLevel.HasValue && entry.Level < query.MinLevel.Value)
            return false;

        if (query.Levels != null && query.Levels.Length > 0 && !query.Levels.Contains(entry.Level))
            return false;

        // Time range filter
        if (query.From.HasValue && entry.Timestamp < query.From.Value)
            return false;

        if (query.To.HasValue && entry.Timestamp > query.To.Value)
            return false;

        // Source filter
        if (query.Sources != null && query.Sources.Length > 0)
        {
            var matchesSource = query.Sources.Any(s =>
                entry.SourceContext?.StartsWith(s, StringComparison.OrdinalIgnoreCase) == true);
            if (!matchesSource) return false;
        }

        // Exception filter
        if (query.HasException == true && entry.Exception == null)
            return false;

        // Request ID filter
        if (!string.IsNullOrEmpty(query.RequestId) &&
            !string.Equals(entry.RequestId, query.RequestId, StringComparison.OrdinalIgnoreCase))
            return false;

        // Search filter
        if (searchRegex != null)
        {
            var matchesMessage = searchRegex.IsMatch(entry.Message);
            var matchesException = entry.Exception?.Message != null && searchRegex.IsMatch(entry.Exception.Message);
            if (!matchesMessage && !matchesException)
                return false;
        }

        return true;
    }

    private static LogEntriesPagedResponse CreatePagedResponse(List<LogEntryDto> entries, LogSearchQuery query)
    {
        var totalCount = entries.Count;
        var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

        // Apply sort order before pagination
        // Entries come from file in chronological order (oldest first)
        // For Newest: reverse to show newest first
        // For Oldest: keep as-is
        IEnumerable<LogEntryDto> sortedEntries = query.SortOrder == LogSortOrder.Newest
            ? entries.AsEnumerable().Reverse()
            : entries;

        var pagedItems = sortedEntries
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new LogEntriesPagedResponse
        {
            Items = pagedItems,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = totalPages
        };
    }
}
