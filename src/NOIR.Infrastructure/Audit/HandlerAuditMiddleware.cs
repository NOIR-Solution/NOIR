using System.Diagnostics;
using System.Text.Json;
using Wolverine;

namespace NOIR.Infrastructure.Audit;

/// <summary>
/// Wolverine middleware for handler-level audit logging with DTO diff tracking.
/// Creates HandlerAuditLog for each handler execution and captures before/after state for auditable commands.
/// </summary>
/// <remarks>
/// This middleware captures:
/// - Handler name and operation type
/// - Input parameters (sanitized)
/// - Output result (sanitized)
/// - DTO diff for IAuditableCommand implementations
/// - Execution timing and success/failure status
///
/// Links to HttpRequestAuditLog via AuditContext.
/// Thread-safe: Uses per-request context keyed by correlation ID.
///
/// Note: Uses synchronous methods to match Wolverine middleware conventions.
/// Database operations use synchronous wrappers since middleware runs in request context.
/// </remarks>
public class HandlerAuditMiddleware
{
    // Instance-level context for this middleware execution
    private readonly Stopwatch _stopwatch = new();
    private HandlerAuditLog? _auditLog;
    private string? _correlationId;

    // DTO diff tracking state
    private object? _beforeState;
    private Type? _dtoType;

    // Sensitive properties to redact from input/output
    private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
        "Secret", "Token", "ApiKey", "PrivateKey", "Salt", "RefreshToken",
        "CreditCard", "CVV", "SSN", "SocialSecurityNumber"
    };

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Called before the handler executes.
    /// Creates the HandlerAuditLog and adds it to the context (saved later in After).
    /// </summary>
    /// <remarks>
    /// Note: This method is synchronous because Wolverine's code generator does not
    /// properly await async middleware methods. The audit log is added to the DbContext
    /// but not saved - it will be saved in the After method.
    /// </remarks>
    public void Before(
        Envelope envelope,
        ApplicationDbContext dbContext,
        IMultiTenantContextAccessor<Tenant> tenantContextAccessor,
        IServiceProvider serviceProvider)
    {
        _stopwatch.Restart();

        var message = envelope.Message;
        if (message is null) return;

        // Skip auditing for handlers with [DisableHandlerAuditing] attribute
        if (message.GetType().GetCustomAttribute<DisableHandlerAuditingAttribute>() is not null)
        {
            return;
        }

        // Only audit IAuditableCommand handlers (Create, Update, Delete operations)
        // Skip queries and other non-mutating operations to save database space
        if (message is not IAuditableCommand auditableCommand)
        {
            return;
        }

        var messageType = message.GetType();
        var handlerName = messageType.Name;

        var operationType = auditableCommand.OperationType;
        var targetId = auditableCommand.GetTargetId();
        var actionDescription = auditableCommand.GetActionDescription();
        var targetDisplayName = auditableCommand.GetTargetDisplayName();

        // Create the handler audit log
        _correlationId = AuditContext.Current?.CorrelationId ?? envelope.CorrelationId ?? Guid.NewGuid().ToString();
        var httpRequestAuditLogId = AuditContext.Current?.HttpRequestAuditLogId;
        var pageContext = AuditContext.Current?.PageContext;

        _auditLog = HandlerAuditLog.Create(
            correlationId: _correlationId,
            handlerName: handlerName,
            operationType: operationType,
            tenantId: tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id,
            httpRequestAuditLogId: httpRequestAuditLogId,
            pageContext: pageContext);

        // Set activity context for timeline display
        _auditLog.SetActivityContext(targetDisplayName, actionDescription);

        // Set target DTO info and fetch before state
        _dtoType = GetTargetDtoTypeFromInterface(messageType);
        var dtoTypeName = _dtoType?.Name ?? GetTargetDtoType(messageType);
        _auditLog.SetTargetDto(dtoTypeName ?? handlerName, targetId?.ToString());

        // Fetch before state for update operations
        if (operationType == AuditOperationType.Update && _dtoType is not null && targetId is not null)
        {
            try
            {
                var beforeStateProvider = serviceProvider.GetService<IBeforeStateProvider>();
                if (beforeStateProvider is not null)
                {
                    // Use synchronous wrapper since Wolverine middleware is synchronous
                    _beforeState = beforeStateProvider
                        .GetBeforeStateAsync(_dtoType, targetId, CancellationToken.None)
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail - before state is optional
                var logger = serviceProvider.GetService<ILogger<HandlerAuditMiddleware>>();
                logger?.LogDebug(ex, "Failed to fetch before state for {DtoType} with ID {TargetId}",
                    _dtoType.Name, targetId);
            }
        }

        // Capture sanitized input parameters
        _auditLog.InputParameters = SanitizeAndSerialize(message);

        // Add to context but don't save yet - will be saved in After method
        dbContext.HandlerAuditLogs.Add(_auditLog);

        // Set the handler ID in audit context so EntityAuditLogInterceptor can link entity changes
        AuditContext.SetCurrentHandler(_auditLog.Id);
    }

    /// <summary>
    /// Called after the handler executes successfully.
    /// Marks the audit log as successful, computes DTO diff, and saves changes.
    /// </summary>
    /// <remarks>
    /// Note: This method is synchronous because Wolverine's code generator does not
    /// properly await async middleware methods. The audit log is saved here along with
    /// any changes made by the handler (if not already saved).
    /// </remarks>
    public void After(
        Envelope envelope,
        ApplicationDbContext dbContext,
        IServiceProvider serviceProvider)
    {
        if (_auditLog is null) return;

        _stopwatch.Stop();

        var dtoDiff = TryComputeDtoDiff(serviceProvider);
        _auditLog.Complete(isSuccess: true, outputResult: null, dtoDiff: dtoDiff);

        TrySaveAuditLog(dbContext, serviceProvider);
        AuditContext.ClearCurrentHandler();
    }

    /// <summary>
    /// Called after the handler executes when returning a Result type.
    /// Detects Result.Failure() and marks the audit accordingly.
    /// </summary>
    public void After(
        Envelope envelope,
        Result result,
        ApplicationDbContext dbContext,
        IServiceProvider serviceProvider)
    {
        if (_auditLog is null) return;

        _stopwatch.Stop();

        if (result.IsFailure)
        {
            _auditLog.Complete(isSuccess: false, errorMessage: result.Error.Message);
        }
        else
        {
            var dtoDiff = TryComputeDtoDiff(serviceProvider);
            _auditLog.Complete(isSuccess: true, outputResult: null, dtoDiff: dtoDiff);
        }

        TrySaveAuditLog(dbContext, serviceProvider);
        AuditContext.ClearCurrentHandler();
    }

    #region Private Helper Methods

    /// <summary>
    /// Attempts to compute the DTO diff if before state was captured.
    /// Returns null if diff cannot be computed (missing state, services, or on error).
    /// </summary>
    private string? TryComputeDtoDiff(IServiceProvider serviceProvider)
    {
        if (_beforeState is null || _dtoType is null || _auditLog?.TargetDtoId is null)
            return null;

        try
        {
            var beforeStateProvider = serviceProvider.GetService<IBeforeStateProvider>();
            var afterState = beforeStateProvider?
                .GetBeforeStateAsync(_dtoType, _auditLog.TargetDtoId, CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            if (afterState is null)
                return null;

            var diffService = serviceProvider.GetService<IDiffService>();
            return diffService is not null
                ? ComputeDtoDiff(diffService, _beforeState, afterState)
                : null;
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetService<ILogger<HandlerAuditMiddleware>>();
            logger?.LogDebug(ex, "Failed to compute DTO diff for {DtoType}", _dtoType?.Name);
            return null;
        }
    }

    /// <summary>
    /// Computes the DTO diff using JSON comparison.
    /// </summary>
    private static string? ComputeDtoDiff(IDiffService diffService, object before, object after)
    {
        var beforeJson = SanitizeAndSerialize(before);
        var afterJson = SanitizeAndSerialize(after);
        return diffService.CreateDiffFromJson(beforeJson, afterJson);
    }

    /// <summary>
    /// Saves the audit log with error handling.
    /// The handler may have already called SaveChanges, but we need to save again
    /// to persist the DtoDiff that was computed after handler execution.
    /// </summary>
    private static void TrySaveAuditLog(ApplicationDbContext dbContext, IServiceProvider serviceProvider)
    {
        try
        {
            // Note: Using async wrapper since DomainEventInterceptor enforces async-only SaveChanges.
            dbContext.SaveChangesAsync(CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetService<ILogger<HandlerAuditMiddleware>>();
            logger?.LogDebug(ex, "Failed to save audit log");
        }
    }

    #endregion

    /// <summary>
    /// Cleanup method called at the end of handler execution.
    /// Checks for uncaught exceptions and marks audit log as failed if needed.
    /// </summary>
    /// <remarks>
    /// Wolverine's middleware pipeline doesn't pass exception info to Finally(),
    /// so we use AuditExceptionContext (set by ExceptionHandlingMiddleware) to detect failures.
    /// </remarks>
    public void Finally(
        Envelope envelope,
        ApplicationDbContext dbContext,
        IServiceProvider serviceProvider)
    {
        _stopwatch.Stop();

        // Check if an exception occurred that wasn't handled by After() methods
        // This catches cases where handlers throw exceptions instead of returning Result.Failure()
        if (_auditLog is not null && _auditLog.IsSuccess)
        {
            var exceptionContext = AuditExceptionContext.Current;
            if (exceptionContext?.Exception is not null)
            {
                var sanitizedMessage = SanitizeExceptionMessage(exceptionContext.Exception);
                _auditLog.Complete(isSuccess: false, errorMessage: sanitizedMessage);

                TrySaveAuditLog(dbContext, serviceProvider);
            }
        }

        // Always clear handler from context
        AuditContext.ClearCurrentHandler();
    }

    /// <summary>
    /// Gets the target DTO type from the command type name.
    /// </summary>
    private static string? GetTargetDtoType(Type messageType)
    {
        // Try to extract DTO type from IAuditableCommand<TDto>
        var dtoType = GetTargetDtoTypeFromInterface(messageType);
        if (dtoType is not null)
        {
            return dtoType.Name;
        }

        // Fallback: infer from command name (e.g., UpdateCustomerCommand -> CustomerDto)
        var name = messageType.Name;
        if (name.EndsWith("Command", StringComparison.Ordinal))
        {
            var entity = name
                .Replace("Create", "", StringComparison.Ordinal)
                .Replace("Update", "", StringComparison.Ordinal)
                .Replace("Delete", "", StringComparison.Ordinal)
                .Replace("Command", "", StringComparison.Ordinal);
            return $"{entity}Dto";
        }

        return null;
    }

    /// <summary>
    /// Gets the DTO type from IAuditableCommand&lt;TDto&gt; interface.
    /// </summary>
    private static Type? GetTargetDtoTypeFromInterface(Type messageType)
    {
        var auditableInterface = messageType
            .GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IAuditableCommand<>));

        return auditableInterface?.GetGenericArguments().FirstOrDefault();
    }

    /// <summary>
    /// Serializes an object to JSON with sensitive properties redacted.
    /// </summary>
    private static string? SanitizeAndSerialize(object? obj)
    {
        if (obj is null) return null;

        try
        {
            var json = JsonSerializer.Serialize(obj, SerializerOptions);
            return SanitizeJson(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Redacts sensitive properties from a JSON string.
    /// </summary>
    private static string SanitizeJson(string json)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                SanitizeJsonElement(writer, element);
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch
        {
            return json;
        }
    }

    private static void SanitizeJsonElement(Utf8JsonWriter writer, JsonElement element, string? propertyName = null)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var prop in element.EnumerateObject())
                {
                    writer.WritePropertyName(prop.Name);
                    if (SensitiveProperties.Any(s => prop.Name.Contains(s, StringComparison.OrdinalIgnoreCase)))
                    {
                        writer.WriteStringValue("[REDACTED]");
                    }
                    else
                    {
                        SanitizeJsonElement(writer, prop.Value, prop.Name);
                    }
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    SanitizeJsonElement(writer, item);
                }
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case JsonValueKind.Number:
                if (element.TryGetInt64(out var l))
                    writer.WriteNumberValue(l);
                else if (element.TryGetDouble(out var d))
                    writer.WriteNumberValue(d);
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
        }
    }

    /// <summary>
    /// Sanitizes exception messages to prevent sensitive data leakage.
    /// Removes common sensitive patterns like connection strings, credentials, and tokens.
    /// </summary>
    private static string SanitizeExceptionMessage(Exception exception)
    {
        var exceptionType = exception.GetType().Name;
        var message = exception.Message ?? "";

        // Patterns to redact (regex-like patterns as simple Contains checks for performance)
        var sensitivePatterns = new[]
        {
            ("password=", "password=[REDACTED]"),
            ("Password=", "Password=[REDACTED]"),
            ("pwd=", "pwd=[REDACTED]"),
            ("secret=", "secret=[REDACTED]"),
            ("Secret=", "Secret=[REDACTED]"),
            ("apikey=", "apikey=[REDACTED]"),
            ("api_key=", "api_key=[REDACTED]"),
            ("ApiKey=", "ApiKey=[REDACTED]"),
            ("token=", "token=[REDACTED]"),
            ("Token=", "Token=[REDACTED]"),
            ("Bearer ", "Bearer [REDACTED]"),
            ("Authorization: ", "Authorization: [REDACTED]"),
            ("connectionstring=", "connectionstring=[REDACTED]"),
            ("Connection String=", "Connection String=[REDACTED]"),
            ("Server=", "[CONNECTION STRING REDACTED]"), // SQL connection strings
            ("Data Source=", "[CONNECTION STRING REDACTED]"),
        };

        foreach (var (pattern, replacement) in sensitivePatterns)
        {
            if (message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                // Replace from the pattern to the end of the word/value
                var index = message.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                var endIndex = message.IndexOfAny([';', ' ', '\n', '\r', '"', '\''], index + pattern.Length);
                if (endIndex == -1) endIndex = message.Length;

                message = string.Concat(message.AsSpan(0, index), replacement, message.AsSpan(endIndex));
            }
        }

        // Truncate long messages to prevent log bloat
        if (message.Length > 500)
        {
            message = message[..500] + "... [TRUNCATED]";
        }

        return $"{exceptionType}: {message}";
    }

}

/// <summary>
/// Interface for fetching "before" state for DTO diff tracking.
/// Implement this to enable automatic before/after diff for update operations.
/// </summary>
public interface IBeforeStateProvider
{
    /// <summary>
    /// Fetches the current state of an entity before modification.
    /// </summary>
    /// <param name="dtoType">The DTO type to fetch.</param>
    /// <param name="targetId">The ID of the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current state as the DTO type, or null if not found.</returns>
    Task<object?> GetBeforeStateAsync(Type dtoType, object targetId, CancellationToken cancellationToken);
}
