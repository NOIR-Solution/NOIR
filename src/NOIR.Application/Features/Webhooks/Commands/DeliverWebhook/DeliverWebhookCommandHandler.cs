namespace NOIR.Application.Features.Webhooks.Commands.DeliverWebhook;

/// <summary>
/// Wolverine handler that performs the actual HTTP delivery of a webhook payload.
/// Signs the payload with HMAC-SHA256, sends the POST request, records the result,
/// and schedules retries with exponential backoff on failure.
/// </summary>
public class DeliverWebhookCommandHandler
{
    /// <summary>
    /// Exponential backoff intervals for retry attempts.
    /// Attempt 1: 30s, Attempt 2: 2m, Attempt 3: 15m, Attempt 4: 1h, Attempt 5: 4h
    /// </summary>
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(4)
    ];

    /// <summary>
    /// Headers that custom webhook subscriptions are not allowed to override.
    /// Prevents header injection attacks and protects internal webhook metadata headers.
    /// </summary>
    private static readonly HashSet<string> BlockedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Host", "Authorization", "Cookie", "Transfer-Encoding",
        "Content-Length", "Content-Type", "Connection",
        "X-Webhook-Signature-256", "X-Webhook-Event", "X-Webhook-Delivery-Id",
        "User-Agent", "Proxy-Authorization", "X-Forwarded-For",
        "X-Forwarded-Host", "X-Real-IP"
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApplicationDbContext _dbContext;
    private readonly IRepository<WebhookSubscription, Guid> _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<DeliverWebhookCommandHandler> _logger;

    public DeliverWebhookCommandHandler(
        IHttpClientFactory httpClientFactory,
        IApplicationDbContext dbContext,
        IRepository<WebhookSubscription, Guid> subscriptionRepository,
        IUnitOfWork unitOfWork,
        IMessageBus messageBus,
        ILogger<DeliverWebhookCommandHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _dbContext = dbContext;
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
        _messageBus = messageBus;
        _logger = logger;
    }

    public async Task Handle(DeliverWebhookCommand command, CancellationToken cancellationToken)
    {
        var deliveryLog = await _dbContext.WebhookDeliveryLogs
            .TagWith("DeliverWebhook_GetDeliveryLog")
            .FirstOrDefaultAsync(l => l.Id == command.DeliveryLogId, cancellationToken);

        if (deliveryLog is null)
        {
            _logger.LogWarning("Delivery log {DeliveryLogId} not found, skipping delivery", command.DeliveryLogId);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient("WebhookDelivery");
            client.Timeout = TimeSpan.FromSeconds(command.TimeoutSeconds);

            // Compute HMAC-SHA256 signature
            var signature = ComputeSignature(command.Payload, command.Secret);

            var request = new HttpRequestMessage(HttpMethod.Post, command.Url)
            {
                Content = new StringContent(command.Payload, Encoding.UTF8, "application/json")
            };

            // Standard webhook headers
            request.Headers.TryAddWithoutValidation("X-Webhook-Signature-256", $"sha256={signature}");
            request.Headers.TryAddWithoutValidation("X-Webhook-Event", command.EventType);
            request.Headers.TryAddWithoutValidation("X-Webhook-Delivery-Id", command.DeliveryLogId.ToString());

            // Custom headers from the subscription
            if (!string.IsNullOrWhiteSpace(command.CustomHeaders))
            {
                try
                {
                    var customHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(command.CustomHeaders);
                    if (customHeaders is not null)
                    {
                        foreach (var (key, value) in customHeaders)
                        {
                            if (BlockedHeaders.Contains(key))
                            {
                                _logger.LogWarning(
                                    "Blocked custom header '{HeaderKey}' for subscription {SubscriptionId} — header is on the blocklist",
                                    key, command.SubscriptionId);
                                continue;
                            }

                            request.Headers.TryAddWithoutValidation(key, value);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse custom headers for subscription {SubscriptionId}", command.SubscriptionId);
                }
            }

            var response = await client.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseHeaders = JsonSerializer.Serialize(
                response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)));

            if (response.IsSuccessStatusCode)
            {
                deliveryLog.RecordSuccess(
                    (int)response.StatusCode,
                    TruncateResponse(responseBody),
                    responseHeaders,
                    stopwatch.ElapsedMilliseconds);

                // Record delivery timestamp on the subscription
                await RecordSubscriptionDelivery(command.SubscriptionId, cancellationToken);

                _logger.LogInformation(
                    "Webhook delivery succeeded for {SubscriptionId} / {DeliveryLogId} ({StatusCode}, {DurationMs}ms)",
                    command.SubscriptionId, command.DeliveryLogId, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                await HandleFailure(
                    command, deliveryLog, (int)response.StatusCode,
                    TruncateResponse(responseBody), responseHeaders,
                    $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                    stopwatch.ElapsedMilliseconds, cancellationToken);
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            await HandleFailure(
                command, deliveryLog, null, null, null,
                $"Request timed out after {command.TimeoutSeconds}s",
                stopwatch.ElapsedMilliseconds, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            await HandleFailure(
                command, deliveryLog, null, null, null,
                $"HTTP request failed: {ex.Message}",
                stopwatch.ElapsedMilliseconds, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Unexpected error delivering webhook for {SubscriptionId} / {DeliveryLogId}",
                command.SubscriptionId, command.DeliveryLogId);

            await HandleFailure(
                command, deliveryLog, null, null, null,
                $"Unexpected error: {ex.Message}",
                stopwatch.ElapsedMilliseconds, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleFailure(
        DeliverWebhookCommand command,
        WebhookDeliveryLog deliveryLog,
        int? statusCode,
        string? responseBody,
        string? responseHeaders,
        string errorMessage,
        long durationMs,
        CancellationToken ct)
    {
        var canRetry = command.AttemptNumber < command.MaxRetries;
        DateTimeOffset? nextRetryAt = null;

        if (canRetry)
        {
            var delayIndex = Math.Min(command.AttemptNumber - 1, RetryDelays.Length - 1);
            nextRetryAt = DateTimeOffset.UtcNow.Add(RetryDelays[delayIndex]);
        }

        deliveryLog.RecordFailure(
            statusCode, responseBody, responseHeaders,
            errorMessage, durationMs, nextRetryAt);

        _logger.LogWarning(
            "Webhook delivery failed for {SubscriptionId} / {DeliveryLogId}: {Error}. Attempt {Attempt}/{MaxRetries}. {RetryInfo}",
            command.SubscriptionId, command.DeliveryLogId, errorMessage,
            command.AttemptNumber, command.MaxRetries,
            canRetry ? $"Retrying at {nextRetryAt:O}" : "No more retries");

        if (canRetry)
        {
            // Schedule a retry via Wolverine scheduled message
            var retryCommand = command with { AttemptNumber = command.AttemptNumber + 1 };
            await _messageBus.ScheduleAsync(retryCommand, nextRetryAt!.Value);
        }
        else
        {
            // Delivery exhausted — consider suspending the subscription after consecutive failures
            _logger.LogWarning(
                "Webhook delivery exhausted all retries for subscription {SubscriptionId}",
                command.SubscriptionId);
        }
    }

    private async Task RecordSubscriptionDelivery(Guid subscriptionId, CancellationToken ct)
    {
        var spec = new Webhooks.Specifications.WebhookSubscriptionByIdForUpdateSpec(subscriptionId);
        var subscription = await _subscriptionRepository.FirstOrDefaultAsync(spec, ct);
        if (subscription is not null)
        {
            subscription.RecordDelivery();
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    private static string ComputeSignature(string payload, string secret)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(secretBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string? TruncateResponse(string? responseBody, int maxLength = 4000)
    {
        if (responseBody is null) return null;
        return responseBody.Length > maxLength ? responseBody[..maxLength] : responseBody;
    }
}
