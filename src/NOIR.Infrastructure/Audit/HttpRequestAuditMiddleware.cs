using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace NOIR.Infrastructure.Audit;

/// <summary>
/// ASP.NET Core middleware that captures HTTP request/response context for audit logging.
/// Creates HttpRequestAuditLog at the start and updates with response at the end.
/// Note: This middleware can be disabled by setting the environment to "Testing".
/// </summary>
public class HttpRequestAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpRequestAuditMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    // Paths to exclude from audit logging
    private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/api/health",
        "/hangfire",
        "/api/docs",
        "/api/openapi",
        "/favicon.ico"
    };

    // Headers to exclude from logging (sensitive)
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "X-Api-Key",
        "X-Auth-Token"
    };

    // Maximum body size to log (to prevent excessive storage)
    private const int MaxBodySizeBytes = 32 * 1024; // 32 KB

    public HttpRequestAuditMiddleware(
        RequestDelegate next,
        ILogger<HttpRequestAuditMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IMultiTenantContextAccessor<Tenant> tenantContextAccessor,
        IAuditBroadcastService auditBroadcast)
    {
        // Disable audit logging in Testing environment to avoid test interference
        if (_environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Skip excluded paths
        var path = context.Request.Path.Value ?? "";
        if (ExcludedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        try
        {
            await ProcessWithAuditingAsync(context, dbContext, currentUser, tenantContextAccessor, auditBroadcast);
        }
        catch (Exception ex)
        {
            // Log but don't fail the request if auditing fails
            _logger.LogError(ex, "HTTP request audit logging failed for {Path}", path);
            await _next(context);
        }
    }

    private async Task ProcessWithAuditingAsync(
        HttpContext context,
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IMultiTenantContextAccessor<Tenant> tenantContextAccessor,
        IAuditBroadcastService auditBroadcast)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = context.TraceIdentifier;

        // Create the HTTP request audit log
        var httpAuditLog = HttpRequestAuditLog.Create(
            correlationId: correlationId,
            httpMethod: context.Request.Method,
            url: context.Request.Path.Value ?? "",
            queryString: context.Request.QueryString.Value,
            userId: currentUser.UserId,
            userEmail: currentUser.Email,
            tenantId: tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id,
            ipAddress: GetClientIpAddress(context),
            userAgent: context.Request.Headers.UserAgent.ToString().Truncate(500));

        // Capture request headers (sanitized)
        httpAuditLog.RequestHeaders = GetSanitizedHeaders(context.Request.Headers);

        // Capture request body (for POST/PUT/PATCH)
        if (context.Request.Method is "POST" or "PUT" or "PATCH" &&
            context.Request.ContentLength > 0 &&
            context.Request.ContentLength < MaxBodySizeBytes)
        {
            httpAuditLog.RequestBody = await ReadRequestBodyAsync(context.Request);
        }

        // Save to get the ID
        dbContext.HttpRequestAuditLogs.Add(httpAuditLog);
        await dbContext.SaveChangesAsync();

        // Start the audit context scope
        using var scope = AuditContext.BeginRequestScope(httpAuditLog.Id, correlationId);

        // Capture response body if needed
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Copy response body back and capture it
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);

            // Capture response body if it's small enough (sanitized)
            if (responseBodyStream.Length > 0 && responseBodyStream.Length < MaxBodySizeBytes)
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                // Use explicit encoding and buffer size to avoid sync I/O in constructor
                var buffer = new byte[(int)responseBodyStream.Length];
                var bytesRead = await responseBodyStream.ReadAsync(buffer.AsMemory());
                var responseBody = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                // CRITICAL: Sanitize response body to prevent sensitive data leakage
                httpAuditLog.ResponseBody = SanitizeBody(responseBody)?.Truncate(MaxBodySizeBytes);
            }

            // Complete the audit log
            httpAuditLog.Complete(context.Response.StatusCode, httpAuditLog.ResponseBody);

            try
            {
                await dbContext.SaveChangesAsync();

                // Broadcast the completed HTTP request audit event
                var handlerCount = await dbContext.HandlerAuditLogs
                    .CountAsync(h => h.CorrelationId == correlationId);
                var entityCount = await dbContext.EntityAuditLogs
                    .CountAsync(e => e.CorrelationId == correlationId);

                var auditEvent = new HttpRequestAuditEvent(
                    httpAuditLog.Id,
                    httpAuditLog.CorrelationId,
                    httpAuditLog.HttpMethod,
                    httpAuditLog.Url,
                    httpAuditLog.ResponseStatusCode,
                    httpAuditLog.UserId,
                    httpAuditLog.UserEmail,
                    httpAuditLog.TenantId,
                    httpAuditLog.IpAddress,
                    httpAuditLog.StartTime,
                    httpAuditLog.DurationMs,
                    handlerCount,
                    entityCount);

                // Fire and forget - don't block the response
                _ = auditBroadcast.BroadcastHttpRequestAuditAsync(auditEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save HTTP request audit log for {CorrelationId}", correlationId);
            }
        }
    }

    private static string GetSanitizedHeaders(IHeaderDictionary headers)
    {
        var sanitized = new Dictionary<string, string>();
        foreach (var header in headers)
        {
            if (SensitiveHeaders.Contains(header.Key))
            {
                sanitized[header.Key] = "[REDACTED]";
            }
            else
            {
                sanitized[header.Key] = header.Value.ToString().Truncate(500) ?? "";
            }
        }
        return JsonSerializer.Serialize(sanitized);
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        request.Body.Position = 0;

        // Sanitize sensitive fields in the body
        return SanitizeBody(body);
    }

    private static string? SanitizeBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            // Try to parse as JSON and redact sensitive fields
            var json = JsonSerializer.Deserialize<JsonElement>(body);
            return SanitizeJsonElement(json);
        }
        catch
        {
            // If not JSON, return as-is (truncated)
            return body.Truncate(MaxBodySizeBytes);
        }
    }

    private static string SanitizeJsonElement(JsonElement element)
    {
        var sensitiveFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "passwordHash", "secret", "token", "apiKey",
            "privateKey", "salt", "refreshToken", "creditCard", "cvv",
            "ssn", "socialSecurityNumber"
        };

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteJsonElement(writer, element, sensitiveFields);
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteJsonElement(Utf8JsonWriter writer, JsonElement element, HashSet<string> sensitiveFields, string? propertyName = null)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var prop in element.EnumerateObject())
                {
                    writer.WritePropertyName(prop.Name);
                    if (sensitiveFields.Contains(prop.Name))
                    {
                        writer.WriteStringValue("[REDACTED]");
                    }
                    else
                    {
                        WriteJsonElement(writer, prop.Value, sensitiveFields, prop.Name);
                    }
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteJsonElement(writer, item, sensitiveFields);
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

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded headers (reverse proxy / load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs: client, proxy1, proxy2
            // The first one is the original client
            var clientIp = forwardedFor.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(clientIp))
                return clientIp;
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Handle null RemoteIpAddress (common in cloud environments like Azure App Service, AWS ALB)
        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp is null)
        {
            return "unknown";
        }

        // MapToIPv4 can throw for some IPv6 addresses, handle gracefully
        try
        {
            return remoteIp.MapToIPv4().ToString();
        }
        catch
        {
            return remoteIp.ToString();
        }
    }
}

/// <summary>
/// Extension methods for string truncation.
/// </summary>
internal static class StringExtensions
{
    public static string? Truncate(this string? value, int maxLength)
    {
        if (value is null) return null;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
