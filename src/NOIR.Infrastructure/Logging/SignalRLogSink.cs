namespace NOIR.Infrastructure.Logging;

using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

/// <summary>
/// Custom Serilog sink that streams log entries to the in-memory buffer and SignalR clients.
/// </summary>
public sealed class SignalRLogSink : ILogEventSink
{
    private readonly ILogRingBuffer _buffer;
    private readonly ILogStreamHubContext _hubContext;
    private readonly DeveloperLogSettings _settings;
    private readonly Regex[] _sensitivePatterns;
    private long _entryCounter;

    public SignalRLogSink(
        ILogRingBuffer buffer,
        ILogStreamHubContext hubContext,
        IOptions<DeveloperLogSettings> options)
    {
        _buffer = buffer;
        _hubContext = hubContext;
        _settings = options.Value;

        // Compile sensitive data patterns
        _sensitivePatterns = _settings.SensitivePatterns
            .Select(p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled))
            .ToArray();
    }

    public void Emit(LogEvent logEvent)
    {
        if (!_settings.Enabled) return;

        try
        {
            var entry = ConvertToDto(logEvent);

            // Add to buffer
            _buffer.Add(entry);

            // Stream to connected clients (async fire-and-forget)
            if (_settings.EnableRealTimeStreaming && _hubContext.GetConnectedClientCount() > 0)
            {
                _ = _hubContext.SendLogEntryAsync(entry, CancellationToken.None);
            }
        }
        catch (Exception)
        {
            // Swallow exceptions to prevent logging failures from crashing the application
            // This is intentional for a logging sink
        }
    }

    private LogEntryDto ConvertToDto(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage();

        // Mask sensitive data if enabled
        if (_settings.MaskSensitiveData)
        {
            message = MaskSensitiveData(message);
        }

        // Truncate if too large
        if (message.Length > _settings.MaxEntrySize)
        {
            message = message[.._settings.MaxEntrySize] + "... [truncated]";
        }

        var entry = new LogEntryDto
        {
            Id = Interlocked.Increment(ref _entryCounter),
            Timestamp = logEvent.Timestamp,
            Level = ConvertLevel(logEvent.Level),
            Message = message,
            MessageTemplate = logEvent.MessageTemplate.Text,
            SourceContext = GetPropertyValue<string>(logEvent, "SourceContext"),
            Exception = ConvertException(logEvent.Exception),
            Properties = ConvertProperties(logEvent.Properties),
            RequestId = GetPropertyValue<string>(logEvent, "RequestId")
                ?? GetPropertyValue<string>(logEvent, "HttpRequestId"),
            TraceId = GetPropertyValue<string>(logEvent, "TraceId"),
            UserId = GetPropertyValue<string>(logEvent, "UserId"),
            TenantId = GetPropertyValue<string>(logEvent, "TenantId")
        };

        return entry;
    }

    private static DevLogLevel ConvertLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => DevLogLevel.Verbose,
        LogEventLevel.Debug => DevLogLevel.Debug,
        LogEventLevel.Information => DevLogLevel.Information,
        LogEventLevel.Warning => DevLogLevel.Warning,
        LogEventLevel.Error => DevLogLevel.Error,
        LogEventLevel.Fatal => DevLogLevel.Fatal,
        _ => DevLogLevel.Information
    };

    private ExceptionDto? ConvertException(Exception? exception)
    {
        if (exception == null) return null;

        var message = exception.Message;
        var stackTrace = exception.StackTrace;

        // Mask sensitive data in exception
        if (_settings.MaskSensitiveData)
        {
            message = MaskSensitiveData(message);
            stackTrace = stackTrace != null ? MaskSensitiveData(stackTrace) : null;
        }

        return new ExceptionDto
        {
            Type = exception.GetType().FullName ?? exception.GetType().Name,
            Message = message,
            StackTrace = stackTrace,
            InnerException = ConvertException(exception.InnerException)
        };
    }

    private IDictionary<string, object?>? ConvertProperties(IReadOnlyDictionary<string, LogEventPropertyValue> properties)
    {
        if (properties.Count == 0) return null;

        var result = new Dictionary<string, object?>();

        foreach (var (key, value) in properties)
        {
            // Skip internal properties
            if (key is "SourceContext" or "RequestId" or "HttpRequestId" or "TraceId" or "UserId" or "TenantId")
                continue;

            var converted = ConvertPropertyValue(value);

            // Mask sensitive data in property values
            if (_settings.MaskSensitiveData && converted is string str)
            {
                converted = MaskSensitiveData(str);
            }

            result[key] = converted;
        }

        return result.Count > 0 ? result : null;
    }

    private static object? ConvertPropertyValue(LogEventPropertyValue value)
    {
        return value switch
        {
            ScalarValue scalar => scalar.Value,
            SequenceValue sequence => sequence.Elements.Select(ConvertPropertyValue).ToArray(),
            StructureValue structure => structure.Properties.ToDictionary(
                p => p.Name,
                p => ConvertPropertyValue(p.Value)),
            DictionaryValue dict => dict.Elements.ToDictionary(
                kvp => ConvertPropertyValue(kvp.Key)?.ToString() ?? "",
                kvp => ConvertPropertyValue(kvp.Value)),
            _ => value.ToString()
        };
    }

    private static T? GetPropertyValue<T>(LogEvent logEvent, string propertyName) where T : class
    {
        if (logEvent.Properties.TryGetValue(propertyName, out var value) && value is ScalarValue scalar)
        {
            return scalar.Value as T;
        }
        return null;
    }

    private string MaskSensitiveData(string text)
    {
        foreach (var pattern in _sensitivePatterns)
        {
            text = pattern.Replace(text, "[REDACTED]");
        }
        return text;
    }
}

/// <summary>
/// A deferred Serilog sink wrapper that can be configured before app.Build() but
/// doesn't require IServiceProvider until initialization.
/// This avoids duplicating Serilog configuration pre- and post-build.
/// </summary>
public sealed class DeferredSignalRLogSink : ILogEventSink
{
    private IServiceProvider? _serviceProvider;
    private SignalRLogSink? _actualSink;
    private readonly object _lock = new();
    private readonly ConcurrentQueue<LogEvent> _pendingEvents = new();
    private const int MaxPendingEvents = 100;

    /// <summary>
    /// Initialize the sink with the service provider after app.Build().
    /// Any events logged before this will be replayed.
    /// </summary>
    public void Initialize(IServiceProvider serviceProvider)
    {
        lock (_lock)
        {
            if (_serviceProvider != null) return; // Already initialized

            _serviceProvider = serviceProvider;

            // Create the actual sink now that we have the service provider
            var buffer = serviceProvider.GetRequiredService<ILogRingBuffer>();
            var hubContext = serviceProvider.GetRequiredService<ILogStreamHubContext>();
            var options = serviceProvider.GetRequiredService<IOptions<DeveloperLogSettings>>();
            _actualSink = new SignalRLogSink(buffer, hubContext, options);

            // Replay any pending events
            while (_pendingEvents.TryDequeue(out var pendingEvent))
            {
                _actualSink.Emit(pendingEvent);
            }
        }
    }

    public void Emit(LogEvent logEvent)
    {
        lock (_lock)
        {
            if (_actualSink != null)
            {
                // Sink is initialized, forward directly
                _actualSink.Emit(logEvent);
            }
            else
            {
                // Queue for replay after initialization (bounded to prevent memory issues)
                if (_pendingEvents.Count < MaxPendingEvents)
                {
                    _pendingEvents.Enqueue(logEvent);
                }
            }
        }
    }
}

/// <summary>
/// Extension methods for configuring SignalR log sink with Serilog.
/// </summary>
public static class SignalRLogSinkExtensions
{
    /// <summary>
    /// Writes log events to the SignalR log stream hub.
    /// This sink should be added after the service provider is built.
    /// </summary>
    public static LoggerConfiguration SignalRLogStream(
        this LoggerSinkConfiguration sinkConfiguration,
        IServiceProvider serviceProvider)
    {
        var buffer = serviceProvider.GetRequiredService<ILogRingBuffer>();
        var hubContext = serviceProvider.GetRequiredService<ILogStreamHubContext>();
        var options = serviceProvider.GetRequiredService<IOptions<DeveloperLogSettings>>();

        var sink = new SignalRLogSink(buffer, hubContext, options);

        return sinkConfiguration.Sink(sink);
    }

    /// <summary>
    /// Writes log events to a deferred SignalR sink that will be initialized after app.Build().
    /// Use this in builder.Host.UseSerilog() and call Initialize() after Build().
    /// </summary>
    public static LoggerConfiguration DeferredSignalRLogStream(
        this LoggerSinkConfiguration sinkConfiguration,
        DeferredSignalRLogSink deferredSink)
    {
        return sinkConfiguration.Sink(deferredSink);
    }
}
