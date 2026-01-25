using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NOIR.Infrastructure.Services.Payment;

/// <summary>
/// Service for logging payment operations to database.
/// Scoped service with in-memory tracking for duration calculation.
/// </summary>
public class PaymentOperationLogger : IPaymentOperationLogger, IScopedService
{
    private readonly IRepository<PaymentOperationLog, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PaymentOperationLogger> _logger;

    // Track operation start times for duration calculation (scoped service - no concurrency needed)
    private readonly Dictionary<Guid, Stopwatch> _operationTimers = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Pre-compiled regex patterns for sensitive field sanitization (performance optimization)
    private static readonly (Regex Regex, string FieldName)[] SensitiveFieldPatterns = CreateSensitiveFieldPatterns();

    private static (Regex, string)[] CreateSensitiveFieldPatterns()
    {
        var patterns = new[]
        {
            "password", "secret", "apikey", "api_key", "accesskey", "access_key",
            "privatekey", "private_key", "token", "credential", "auth",
            "hashsecret", "hash_secret", "key1", "key2", "secretkey", "secret_key"
        };

        return patterns.Select(pattern =>
            (new Regex(
                $@"""{pattern}""\s*:\s*""[^""]*""",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
             pattern))
            .ToArray();
    }

    public PaymentOperationLogger(
        IRepository<PaymentOperationLog, Guid> repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PaymentOperationLogger> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Guid> StartOperationAsync(
        PaymentOperationType operationType,
        string provider,
        string? transactionNumber = null,
        Guid? paymentTransactionId = null,
        Guid? refundId = null,
        CancellationToken cancellationToken = default)
    {
        var correlationId = GetCorrelationId();
        var tenantId = _currentUser.TenantId;

        var log = PaymentOperationLog.Create(operationType, provider, correlationId, tenantId);
        log.SetTransactionInfo(paymentTransactionId, transactionNumber);

        if (refundId.HasValue)
        {
            log.SetRefundInfo(refundId.Value);
        }

        log.SetUserInfo(_currentUser.UserId, GetClientIpAddress());

        await _repository.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Start timer for duration tracking
        var stopwatch = Stopwatch.StartNew();
        _operationTimers[log.Id] = stopwatch;

        _logger.LogDebug(
            "Started payment operation {OperationType} for provider {Provider}, CorrelationId: {CorrelationId}",
            operationType, provider, correlationId);

        return log.Id;
    }

    public async Task SetRequestDataAsync(
        Guid operationLogId,
        object? requestData,
        CancellationToken cancellationToken = default)
    {
        var log = await GetLogAsync(operationLogId, cancellationToken);
        if (log == null)
        {
            _logger.LogWarning(
                "Cannot set request data - operation log {OperationLogId} not found",
                operationLogId);
            return;
        }

        var sanitizedData = SanitizeAndSerialize(requestData);
        log.SetRequestData(sanitizedData);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteSuccessAsync(
        Guid operationLogId,
        object? responseData = null,
        int? httpStatusCode = null,
        CancellationToken cancellationToken = default)
    {
        var log = await GetLogAsync(operationLogId, cancellationToken);
        if (log == null)
        {
            _logger.LogWarning(
                "Cannot complete success - operation log {OperationLogId} not found",
                operationLogId);
            return;
        }

        var duration = GetAndStopTimer(operationLogId);
        log.SetDuration(duration);
        log.SetResponseData(SanitizeAndSerialize(responseData), httpStatusCode);
        log.MarkAsSuccess();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Payment operation {OperationLogId} completed successfully in {DurationMs}ms",
            operationLogId, duration);
    }

    public async Task CompleteFailedAsync(
        Guid operationLogId,
        string? errorCode,
        string? errorMessage,
        object? responseData = null,
        int? httpStatusCode = null,
        Exception? exception = null,
        CancellationToken cancellationToken = default)
    {
        var log = await GetLogAsync(operationLogId, cancellationToken);
        if (log == null)
        {
            _logger.LogWarning(
                "Cannot complete failed - operation log {OperationLogId} not found",
                operationLogId);
            return;
        }

        var duration = GetAndStopTimer(operationLogId);
        log.SetDuration(duration);
        log.SetResponseData(SanitizeAndSerialize(responseData), httpStatusCode);
        log.MarkAsFailed(errorCode, errorMessage, exception?.ToString());

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Payment operation {OperationLogId} failed in {DurationMs}ms: {ErrorCode} - {ErrorMessage}",
            operationLogId, duration, errorCode, errorMessage);
    }

    public async Task AddContextAsync(
        Guid operationLogId,
        object context,
        CancellationToken cancellationToken = default)
    {
        var log = await GetLogAsync(operationLogId, cancellationToken);
        if (log == null)
        {
            _logger.LogWarning(
                "Cannot add context - operation log {OperationLogId} not found",
                operationLogId);
            return;
        }

        log.SetAdditionalContext(SanitizeAndSerialize(context));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task LogOperationAsync(
        PaymentOperationType operationType,
        string provider,
        bool success,
        string? transactionNumber = null,
        Guid? paymentTransactionId = null,
        object? requestData = null,
        object? responseData = null,
        int? httpStatusCode = null,
        long? durationMs = null,
        string? errorCode = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var correlationId = GetCorrelationId();
        var tenantId = _currentUser.TenantId;

        var log = PaymentOperationLog.Create(operationType, provider, correlationId, tenantId);
        log.SetTransactionInfo(paymentTransactionId, transactionNumber);
        log.SetUserInfo(_currentUser.UserId, GetClientIpAddress());
        log.SetRequestData(SanitizeAndSerialize(requestData));
        log.SetResponseData(SanitizeAndSerialize(responseData), httpStatusCode);
        log.SetDuration(durationMs ?? 0);

        if (success)
        {
            log.MarkAsSuccess();
        }
        else
        {
            log.MarkAsFailed(errorCode, errorMessage);
        }

        await _repository.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (success)
        {
            _logger.LogDebug(
                "Payment operation {OperationType} for {Provider} logged successfully",
                operationType, provider);
        }
        else
        {
            _logger.LogWarning(
                "Payment operation {OperationType} for {Provider} logged as failed: {ErrorMessage}",
                operationType, provider, errorMessage);
        }
    }

    private async Task<PaymentOperationLog?> GetLogAsync(Guid id, CancellationToken cancellationToken)
    {
        var spec = new PaymentOperationLogByIdForUpdateSpec(id);
        return await _repository.FirstOrDefaultAsync(spec, cancellationToken);
    }

    private long GetAndStopTimer(Guid operationLogId)
    {
        if (_operationTimers.Remove(operationLogId, out var stopwatch))
        {
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }
        return 0;
    }

    private string GetCorrelationId()
    {
        // Try to get from HTTP context (if in web request)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            // Check for existing correlation ID header
            if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId) &&
                !string.IsNullOrEmpty(correlationId))
            {
                return correlationId!;
            }

            // Use trace identifier
            return httpContext.TraceIdentifier;
        }

        // Generate new one for background jobs
        return Guid.NewGuid().ToString("N");
    }

    private string? GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        // Check for forwarded header first (for proxies/load balancers)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static string? SanitizeAndSerialize(object? data)
    {
        if (data == null) return null;

        try
        {
            // If already a string, use as-is
            if (data is string str) return str;

            // Serialize to JSON
            var json = JsonSerializer.Serialize(data, JsonOptions);

            // Sanitize sensitive fields
            return SanitizeSensitiveData(json);
        }
        catch (Exception)
        {
            return data.ToString();
        }
    }

    private static string SanitizeSensitiveData(string json)
    {
        // Use pre-compiled regex patterns for better performance
        foreach (var (regex, fieldName) in SensitiveFieldPatterns)
        {
            json = regex.Replace(json, $@"""{fieldName}"": ""***REDACTED***""");
        }

        return json;
    }
}
