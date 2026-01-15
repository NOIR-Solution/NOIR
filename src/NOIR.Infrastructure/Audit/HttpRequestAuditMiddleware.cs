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

    private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/api/health",
        "/hangfire",
        "/api/docs",
        "/api/openapi",
        "/favicon.ico"
    };

    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "X-Api-Key",
        "X-Auth-Token"
    };

    private const int MaxBodySizeBytes = 32 * 1024;

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
        IMultiTenantContextAccessor<Tenant> tenantContextAccessor)
    {
        if (_environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";
        if (ExcludedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        await ProcessWithAuditingAsync(context, dbContext, currentUser, tenantContextAccessor);
    }

    private async Task ProcessWithAuditingAsync(
        HttpContext context,
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IMultiTenantContextAccessor<Tenant> tenantContextAccessor)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = context.TraceIdentifier;

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

        httpAuditLog.RequestHeaders = GetSanitizedHeaders(context.Request.Headers);

        if (context.Request.Method is "POST" or "PUT" or "PATCH" &&
            context.Request.ContentLength > 0 &&
            context.Request.ContentLength < MaxBodySizeBytes)
        {
            httpAuditLog.RequestBody = await ReadRequestBodyAsync(context.Request);
        }

        dbContext.HttpRequestAuditLogs.Add(httpAuditLog);
        await dbContext.SaveChangesAsync();

        // Extract page context from frontend header for activity timeline display
        var pageContext = context.Request.Headers["X-Page-Context"].FirstOrDefault();

        using var scope = AuditContext.BeginRequestScope(httpAuditLog.Id, correlationId, pageContext);

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

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);

            if (responseBodyStream.Length > 0 && responseBodyStream.Length < MaxBodySizeBytes)
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var buffer = new byte[(int)responseBodyStream.Length];
                var bytesRead = await responseBodyStream.ReadAsync(buffer.AsMemory());
                var responseBody = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                httpAuditLog.ResponseBody = SanitizeBody(responseBody)?.Truncate(MaxBodySizeBytes);
            }

            httpAuditLog.Complete(context.Response.StatusCode, httpAuditLog.ResponseBody);

            try
            {
                await dbContext.SaveChangesAsync();
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

        return SanitizeBody(body);
    }

    private static string? SanitizeBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(body);
            return SanitizeJsonElement(json);
        }
        catch
        {
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
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var clientIp = forwardedFor.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(clientIp))
                return clientIp;
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp is null)
        {
            return "unknown";
        }

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

internal static class StringExtensions
{
    public static string? Truncate(this string? value, int maxLength)
    {
        if (value is null) return null;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
