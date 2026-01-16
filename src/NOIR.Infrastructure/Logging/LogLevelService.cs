namespace NOIR.Infrastructure.Logging;

using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using NOIR.Application.Common.Interfaces;
using NOIR.Application.Features.DeveloperLogs.DTOs;
using Serilog.Core;
using Serilog.Events;

/// <summary>
/// Service for managing Serilog log levels dynamically at runtime.
/// </summary>
public sealed class LogLevelService : ILogLevelService, ISingletonService
{
    private readonly LoggingLevelSwitch _globalLevelSwitch;
    private readonly ConcurrentDictionary<string, LoggingLevelSwitch> _sourceOverrides;
    private readonly ILogger<LogLevelService> _logger;

    public event Action<string>? OnLevelChanged;

    public LogLevelService(
        LoggingLevelSwitch globalLevelSwitch,
        IOptions<DeveloperLogSettings> options,
        ILogger<LogLevelService> logger)
    {
        _globalLevelSwitch = globalLevelSwitch;
        _sourceOverrides = new ConcurrentDictionary<string, LoggingLevelSwitch>();
        _logger = logger;

        // Initialize with configured default level
        var defaultLevel = ParseLevel(options.Value.DefaultMinimumLevel) ?? LogEventLevel.Information;
        _globalLevelSwitch.MinimumLevel = defaultLevel;

        // Initialize source overrides from configuration
        foreach (var (source, level) in options.Value.LevelOverrides)
        {
            var parsedLevel = ParseLevel(level);
            if (parsedLevel.HasValue)
            {
                var levelSwitch = new LoggingLevelSwitch(parsedLevel.Value);
                _sourceOverrides.TryAdd(source, levelSwitch);
            }
        }
    }

    /// <inheritdoc />
    public string GetCurrentLevel()
    {
        return _globalLevelSwitch.MinimumLevel.ToString();
    }

    /// <inheritdoc />
    public bool SetLevel(string level)
    {
        var parsedLevel = ParseLevel(level);
        if (!parsedLevel.HasValue)
        {
            _logger.LogWarning("Invalid log level requested: {Level}", level);
            return false;
        }

        var previousLevel = _globalLevelSwitch.MinimumLevel;
        _globalLevelSwitch.MinimumLevel = parsedLevel.Value;

        _logger.LogInformation(
            "Global log level changed from {PreviousLevel} to {NewLevel}",
            previousLevel, parsedLevel.Value);

        OnLevelChanged?.Invoke(parsedLevel.Value.ToString());
        return true;
    }

    /// <inheritdoc />
    public string[] GetAvailableLevels()
    {
        return Enum.GetNames<LogEventLevel>();
    }

    /// <inheritdoc />
    public IEnumerable<LogLevelOverrideDto> GetOverrides()
    {
        return _sourceOverrides
            .Select(kvp => new LogLevelOverrideDto(kvp.Key, kvp.Value.MinimumLevel.ToString()))
            .OrderBy(o => o.SourcePrefix);
    }

    /// <inheritdoc />
    public bool SetOverride(string sourcePrefix, string level)
    {
        if (string.IsNullOrWhiteSpace(sourcePrefix))
        {
            return false;
        }

        var parsedLevel = ParseLevel(level);
        if (!parsedLevel.HasValue)
        {
            _logger.LogWarning("Invalid log level for override: {Level}", level);
            return false;
        }

        _sourceOverrides.AddOrUpdate(
            sourcePrefix,
            _ => new LoggingLevelSwitch(parsedLevel.Value),
            (_, existing) =>
            {
                existing.MinimumLevel = parsedLevel.Value;
                return existing;
            });

        _logger.LogInformation(
            "Log level override set for source '{Source}' to {Level}",
            sourcePrefix, parsedLevel.Value);

        return true;
    }

    /// <inheritdoc />
    public bool RemoveOverride(string sourcePrefix)
    {
        if (_sourceOverrides.TryRemove(sourcePrefix, out _))
        {
            _logger.LogInformation("Log level override removed for source '{Source}'", sourcePrefix);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the LoggingLevelSwitch for the global level (used by Serilog configuration).
    /// </summary>
    public LoggingLevelSwitch GetGlobalSwitch() => _globalLevelSwitch;

    /// <summary>
    /// Gets a LoggingLevelSwitch for a specific source prefix (used by Serilog configuration).
    /// </summary>
    public LoggingLevelSwitch? GetSourceSwitch(string sourcePrefix)
    {
        return _sourceOverrides.TryGetValue(sourcePrefix, out var levelSwitch) ? levelSwitch : null;
    }

    /// <summary>
    /// Gets all source overrides for Serilog configuration.
    /// </summary>
    public IReadOnlyDictionary<string, LoggingLevelSwitch> GetAllSourceSwitches()
    {
        return _sourceOverrides;
    }

    private static LogEventLevel? ParseLevel(string level)
    {
        if (Enum.TryParse<LogEventLevel>(level, ignoreCase: true, out var result))
        {
            return result;
        }

        // Handle common aliases
        return level.ToUpperInvariant() switch
        {
            "TRACE" => LogEventLevel.Verbose,
            "DBG" => LogEventLevel.Debug,
            "INF" or "INFO" => LogEventLevel.Information,
            "WRN" or "WARN" => LogEventLevel.Warning,
            "ERR" => LogEventLevel.Error,
            "FTL" or "CRIT" or "CRITICAL" => LogEventLevel.Fatal,
            _ => null
        };
    }
}
