namespace NOIR.Infrastructure.Logging;

/// <summary>
/// Thread-safe circular buffer for log entries with fixed memory footprint.
/// </summary>
public sealed class LogRingBuffer : ILogRingBuffer, ISingletonService
{
    private readonly LogEntryDto[] _buffer;
    private readonly int _capacity;
    private readonly object _lock = new();
    private long _nextId = 1;
    private int _head = 0; // Points to the oldest entry
    private int _count = 0;

    // Statistics tracking
    private readonly ConcurrentDictionary<DevLogLevel, int> _levelCounts = new();
    private long _estimatedMemoryBytes = 0;

    // Error clustering
    private readonly ConcurrentDictionary<string, ErrorClusterState> _errorClusters = new();

    public event Action<LogEntryDto>? OnEntryAdded;

    public LogRingBuffer(IOptions<DeveloperLogSettings> options)
    {
        _capacity = options.Value.BufferCapacity;
        _buffer = new LogEntryDto[_capacity];

        // Initialize level counts
        foreach (DevLogLevel level in Enum.GetValues<DevLogLevel>())
        {
            _levelCounts[level] = 0;
        }
    }

    /// <inheritdoc />
    public void Add(LogEntryDto entry)
    {
        lock (_lock)
        {
            // If buffer is full, we're replacing the oldest entry
            if (_count == _capacity)
            {
                var oldEntry = _buffer[_head];
                if (oldEntry != null)
                {
                    // Decrement count for old entry's level
                    _levelCounts.AddOrUpdate(oldEntry.Level, 0, (_, c) => Math.Max(0, c - 1));
                    _estimatedMemoryBytes -= EstimateEntrySize(oldEntry);
                }
            }

            // Create entry with assigned ID
            var entryWithId = entry with { Id = _nextId++ };

            // Calculate insertion position
            var insertPos = (_head + _count) % _capacity;
            if (_count == _capacity)
            {
                // Buffer is full, advance head
                insertPos = _head;
                _head = (_head + 1) % _capacity;
            }
            else
            {
                _count++;
            }

            _buffer[insertPos] = entryWithId;

            // Update statistics
            _levelCounts.AddOrUpdate(entryWithId.Level, 1, (_, c) => c + 1);
            _estimatedMemoryBytes += EstimateEntrySize(entryWithId);

            // Update error clusters
            if (entryWithId.Level >= DevLogLevel.Error)
            {
                UpdateErrorCluster(entryWithId);
            }
        }

        // Raise event outside lock to avoid deadlocks
        OnEntryAdded?.Invoke(entry);
    }

    /// <inheritdoc />
    public IEnumerable<LogEntryDto> GetRecentEntries(int count)
    {
        lock (_lock)
        {
            if (_count == 0) return Enumerable.Empty<LogEntryDto>();

            var actualCount = Math.Min(count, _count);
            var result = new List<LogEntryDto>(actualCount);

            // Start from most recent and work backwards
            var startIndex = (_head + _count - 1) % _capacity;
            for (var i = 0; i < actualCount; i++)
            {
                var index = (startIndex - i + _capacity) % _capacity;
                if (_buffer[index] != null)
                {
                    result.Add(_buffer[index]);
                }
            }

            return result; // Returns newest first
        }
    }

    /// <inheritdoc />
    public IEnumerable<LogEntryDto> GetEntriesBefore(long beforeId, int count)
    {
        lock (_lock)
        {
            if (_count == 0) return Enumerable.Empty<LogEntryDto>();

            var result = new List<LogEntryDto>();

            // Find entries with ID less than beforeId
            for (var i = 0; i < _count && result.Count < count; i++)
            {
                var index = (_head + _count - 1 - i + _capacity) % _capacity;
                var entry = _buffer[index];
                if (entry != null && entry.Id < beforeId)
                {
                    result.Add(entry);
                }
            }

            return result;
        }
    }

    /// <inheritdoc />
    public IEnumerable<LogEntryDto> GetFiltered(
        DevLogLevel? minLevel = null,
        string[]? sources = null,
        string? searchPattern = null,
        bool exceptionsOnly = false,
        int maxCount = 1000)
    {
        Regex? searchRegex = null;
        if (!string.IsNullOrEmpty(searchPattern))
        {
            try
            {
                searchRegex = new Regex(searchPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            catch
            {
                // If regex is invalid, treat as literal string
                searchRegex = new Regex(Regex.Escape(searchPattern), RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
        }

        lock (_lock)
        {
            var result = new List<LogEntryDto>();

            // Iterate from newest to oldest
            for (var i = 0; i < _count && result.Count < maxCount; i++)
            {
                var index = (_head + _count - 1 - i + _capacity) % _capacity;
                var entry = _buffer[index];

                if (entry == null) continue;

                // Apply filters
                if (minLevel.HasValue && entry.Level < minLevel.Value) continue;
                if (exceptionsOnly && entry.Exception == null) continue;

                if (sources != null && sources.Length > 0)
                {
                    var matchesSource = sources.Any(s =>
                        entry.SourceContext?.StartsWith(s, StringComparison.OrdinalIgnoreCase) == true);
                    if (!matchesSource) continue;
                }

                if (searchRegex != null)
                {
                    if (!searchRegex.IsMatch(entry.Message) &&
                        (entry.Exception?.Message == null || !searchRegex.IsMatch(entry.Exception.Message)))
                    {
                        continue;
                    }
                }

                result.Add(entry);
            }

            return result;
        }
    }

    /// <inheritdoc />
    public IEnumerable<ErrorClusterDto> GetErrorClusters(int maxClusters = 10)
    {
        return _errorClusters.Values
            .OrderByDescending(c => c.Count)
            .Take(maxClusters)
            .Select(c => new ErrorClusterDto
            {
                Id = c.Id,
                Pattern = c.Pattern,
                Count = c.Count,
                FirstSeen = c.FirstSeen,
                LastSeen = c.LastSeen,
                Samples = c.Samples.ToArray(),
                Severity = DetermineSeverity(c)
            });
    }

    /// <inheritdoc />
    public LogBufferStatsDto GetStats()
    {
        lock (_lock)
        {
            DateTimeOffset? oldest = null;
            DateTimeOffset? newest = null;

            if (_count > 0)
            {
                oldest = _buffer[_head]?.Timestamp;
                var newestIndex = (_head + _count - 1) % _capacity;
                newest = _buffer[newestIndex]?.Timestamp;
            }

            return new LogBufferStatsDto
            {
                TotalEntries = _count,
                MaxCapacity = _capacity,
                EntriesByLevel = _levelCounts
                    .Where(kvp => kvp.Value > 0)
                    .ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
                MemoryUsageBytes = _estimatedMemoryBytes,
                OldestEntry = oldest,
                NewestEntry = newest
            };
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _head = 0;
            _count = 0;
            _estimatedMemoryBytes = 0;

            foreach (var level in _levelCounts.Keys.ToList())
            {
                _levelCounts[level] = 0;
            }
        }

        _errorClusters.Clear();
    }

    private void UpdateErrorCluster(LogEntryDto entry)
    {
        var pattern = NormalizeErrorMessage(entry.Exception?.Message ?? entry.Message);
        var hash = ComputePatternHash(pattern);

        _errorClusters.AddOrUpdate(
            hash,
            _ => new ErrorClusterState
            {
                Id = hash,
                Pattern = pattern.Length > 200 ? pattern[..200] + "..." : pattern,
                Count = 1,
                FirstSeen = entry.Timestamp,
                LastSeen = entry.Timestamp,
                Samples = new List<LogEntryDto> { entry }
            },
            (_, existing) =>
            {
                existing.Count++;
                existing.LastSeen = entry.Timestamp;
                if (existing.Samples.Count < 5)
                {
                    existing.Samples.Add(entry);
                }
                return existing;
            });
    }

    private static string NormalizeErrorMessage(string message)
    {
        // Replace variable parts with placeholders
        var normalized = message;
        normalized = Regex.Replace(normalized, @"\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b", "<UUID>", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b", "<IP>");
        normalized = Regex.Replace(normalized, @"\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2}", "<TIMESTAMP>");
        normalized = Regex.Replace(normalized, @"\b\d+\b", "<NUM>");
        normalized = Regex.Replace(normalized, @"'[^']*'", "'<STR>'");
        normalized = Regex.Replace(normalized, @"""[^""]*""", "\"<STR>\"");
        return normalized;
    }

    private static string ComputePatternHash(string pattern)
    {
        var bytes = Encoding.UTF8.GetBytes(pattern);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes)[..16];
    }

    private static string DetermineSeverity(ErrorClusterState cluster)
    {
        if (cluster.Count > 100) return "critical";
        if (cluster.Count > 20) return "high";
        if (cluster.Count > 5) return "medium";
        return "low";
    }

    private static long EstimateEntrySize(LogEntryDto entry)
    {
        // Rough estimation of memory usage
        var size = 100L; // Base object overhead
        size += (entry.Message?.Length ?? 0) * 2;
        size += (entry.MessageTemplate?.Length ?? 0) * 2;
        size += (entry.SourceContext?.Length ?? 0) * 2;
        size += (entry.Exception?.Type.Length ?? 0) * 2;
        size += (entry.Exception?.Message.Length ?? 0) * 2;
        size += (entry.Exception?.StackTrace?.Length ?? 0) * 2;
        return size;
    }

    private class ErrorClusterState
    {
        public required string Id { get; init; }
        public required string Pattern { get; init; }
        public int Count { get; set; }
        public DateTimeOffset FirstSeen { get; set; }
        public DateTimeOffset LastSeen { get; set; }
        public List<LogEntryDto> Samples { get; init; } = new();
    }
}
