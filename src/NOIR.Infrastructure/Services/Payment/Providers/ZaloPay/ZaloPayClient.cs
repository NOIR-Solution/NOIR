using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace NOIR.Infrastructure.Services.Payment.Providers.ZaloPay;

/// <summary>
/// HTTP client for ZaloPay API operations.
/// </summary>
public class ZaloPayClient : IZaloPayClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZaloPayClient> _logger;

    public ZaloPayClient(HttpClient httpClient, ILogger<ZaloPayClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Creates an order with ZaloPay.
    /// </summary>
    public async Task<ZaloPayOrderResponse?> CreateOrderAsync(
        ZaloPayOrderRequest request,
        string? endpoint = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["app_id"] = request.AppId,
                ["app_user"] = request.AppUser,
                ["app_trans_id"] = request.AppTransId,
                ["app_time"] = request.AppTime.ToString(),
                ["amount"] = request.Amount.ToString(),
                ["item"] = request.Item,
                ["description"] = request.Description,
                ["embed_data"] = request.EmbedData,
                ["bank_code"] = request.BankCode,
                ["callback_url"] = request.CallbackUrl,
                ["mac"] = request.Mac
            });

            // Use provided endpoint for per-tenant configuration, or fall back to HttpClient.BaseAddress
            var requestUri = string.IsNullOrEmpty(endpoint)
                ? "create"
                : $"{endpoint.TrimEnd('/')}/create";

            var response = await _httpClient.PostAsync(requestUri, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ZaloPayOrderResponse>(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to create ZaloPay order {AppTransId}", request.AppTransId);
            throw;
        }
    }

    /// <summary>
    /// Queries the status of an order from ZaloPay.
    /// </summary>
    public async Task<ZaloPayQueryResponse?> QueryOrderAsync(
        ZaloPayQueryRequest request,
        string? endpoint = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["app_id"] = request.AppId,
                ["app_trans_id"] = request.AppTransId,
                ["mac"] = request.Mac
            });

            // Use provided endpoint for per-tenant configuration, or fall back to HttpClient.BaseAddress
            var requestUri = string.IsNullOrEmpty(endpoint)
                ? "query"
                : $"{endpoint.TrimEnd('/')}/query";

            var response = await _httpClient.PostAsync(requestUri, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ZaloPayQueryResponse>(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to query ZaloPay order {AppTransId}", request.AppTransId);
            throw;
        }
    }

    /// <summary>
    /// Requests a refund from ZaloPay.
    /// </summary>
    public async Task<ZaloPayRefundResponse?> RefundAsync(
        ZaloPayRefundRequest request,
        string? endpoint = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["app_id"] = request.AppId,
                ["zp_trans_id"] = request.ZpTransId,
                ["amount"] = request.Amount.ToString(),
                ["description"] = request.Description,
                ["timestamp"] = request.Timestamp.ToString(),
                ["mac"] = request.Mac
            });

            // Use provided endpoint for per-tenant configuration, or fall back to HttpClient.BaseAddress
            var requestUri = string.IsNullOrEmpty(endpoint)
                ? "refund"
                : $"{endpoint.TrimEnd('/')}/refund";

            var response = await _httpClient.PostAsync(requestUri, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ZaloPayRefundResponse>(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to process ZaloPay refund for {ZpTransId}", request.ZpTransId);
            throw;
        }
    }
}

/// <summary>
/// Interface for ZaloPay HTTP client.
/// </summary>
public interface IZaloPayClient
{
    Task<ZaloPayOrderResponse?> CreateOrderAsync(ZaloPayOrderRequest request, string? endpoint = null, CancellationToken cancellationToken = default);
    Task<ZaloPayQueryResponse?> QueryOrderAsync(ZaloPayQueryRequest request, string? endpoint = null, CancellationToken cancellationToken = default);
    Task<ZaloPayRefundResponse?> RefundAsync(ZaloPayRefundRequest request, string? endpoint = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for ZaloPay order creation.
/// </summary>
public class ZaloPayOrderRequest
{
    public string AppId { get; set; } = string.Empty;
    public string AppUser { get; set; } = string.Empty;
    public string AppTransId { get; set; } = string.Empty;
    public long AppTime { get; set; }
    public long Amount { get; set; }
    public string Item { get; set; } = "[]";
    public string Description { get; set; } = string.Empty;
    public string EmbedData { get; set; } = "{}";
    public string BankCode { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string Mac { get; set; } = string.Empty;
}

/// <summary>
/// Response model for ZaloPay order creation.
/// </summary>
public class ZaloPayOrderResponse
{
    [JsonPropertyName("return_code")]
    public int ReturnCode { get; set; }

    [JsonPropertyName("return_message")]
    public string? ReturnMessage { get; set; }

    [JsonPropertyName("sub_return_code")]
    public int SubReturnCode { get; set; }

    [JsonPropertyName("sub_return_message")]
    public string? SubReturnMessage { get; set; }

    [JsonPropertyName("order_url")]
    public string? OrderUrl { get; set; }

    [JsonPropertyName("zp_trans_token")]
    public string? ZpTransToken { get; set; }

    [JsonPropertyName("order_token")]
    public string? OrderToken { get; set; }

    [JsonPropertyName("qr_code")]
    public string? QrCode { get; set; }
}

/// <summary>
/// Request model for ZaloPay order query.
/// </summary>
public class ZaloPayQueryRequest
{
    public string AppId { get; set; } = string.Empty;
    public string AppTransId { get; set; } = string.Empty;
    public string Mac { get; set; } = string.Empty;
}

/// <summary>
/// Response model for ZaloPay order query.
/// </summary>
public class ZaloPayQueryResponse
{
    [JsonPropertyName("return_code")]
    public int ReturnCode { get; set; }

    [JsonPropertyName("return_message")]
    public string? ReturnMessage { get; set; }

    [JsonPropertyName("is_processing")]
    public bool IsProcessing { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("zp_trans_id")]
    public long ZpTransId { get; set; }

    [JsonPropertyName("server_time")]
    public long ServerTime { get; set; }

    [JsonPropertyName("discount_amount")]
    public long DiscountAmount { get; set; }
}

/// <summary>
/// Request model for ZaloPay refund.
/// </summary>
public class ZaloPayRefundRequest
{
    public string AppId { get; set; } = string.Empty;
    public string ZpTransId { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public string Mac { get; set; } = string.Empty;
}

/// <summary>
/// Response model for ZaloPay refund.
/// </summary>
public class ZaloPayRefundResponse
{
    [JsonPropertyName("return_code")]
    public int ReturnCode { get; set; }

    [JsonPropertyName("return_message")]
    public string? ReturnMessage { get; set; }

    [JsonPropertyName("sub_return_code")]
    public int SubReturnCode { get; set; }

    [JsonPropertyName("sub_return_message")]
    public string? SubReturnMessage { get; set; }

    [JsonPropertyName("refund_id")]
    public long RefundId { get; set; }
}

/// <summary>
/// Model for ZaloPay callback notification.
/// </summary>
public class ZaloPayCallbackData
{
    [JsonPropertyName("data")]
    public string? Data { get; set; }

    [JsonPropertyName("mac")]
    public string? Mac { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }
}

/// <summary>
/// Parsed data from ZaloPay callback.
/// </summary>
public class ZaloPayCallbackPayload
{
    [JsonPropertyName("app_id")]
    public int AppId { get; set; }

    [JsonPropertyName("app_trans_id")]
    public string? AppTransId { get; set; }

    [JsonPropertyName("app_time")]
    public long AppTime { get; set; }

    [JsonPropertyName("app_user")]
    public string? AppUser { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("embed_data")]
    public string? EmbedData { get; set; }

    [JsonPropertyName("item")]
    public string? Item { get; set; }

    [JsonPropertyName("zp_trans_id")]
    public long ZpTransId { get; set; }

    [JsonPropertyName("server_time")]
    public long ServerTime { get; set; }

    [JsonPropertyName("channel")]
    public int Channel { get; set; }

    [JsonPropertyName("merchant_user_id")]
    public string? MerchantUserId { get; set; }

    [JsonPropertyName("user_fee_amount")]
    public long UserFeeAmount { get; set; }

    [JsonPropertyName("discount_amount")]
    public long DiscountAmount { get; set; }
}
