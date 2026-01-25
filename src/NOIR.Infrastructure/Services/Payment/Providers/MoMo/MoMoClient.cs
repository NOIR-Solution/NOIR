using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace NOIR.Infrastructure.Services.Payment.Providers.MoMo;

/// <summary>
/// HTTP client for MoMo API operations.
/// </summary>
public class MoMoClient : IMoMoClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MoMoClient> _logger;

    public MoMoClient(HttpClient httpClient, ILogger<MoMoClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Creates a payment request with MoMo.
    /// </summary>
    public async Task<MoMoPaymentResponse?> CreatePaymentAsync(
        MoMoPaymentRequest request,
        string? apiEndpoint = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use provided apiEndpoint for per-tenant configuration, or fall back to HttpClient.BaseAddress
            var requestUri = string.IsNullOrEmpty(apiEndpoint)
                ? "create"
                : $"{apiEndpoint.TrimEnd('/')}/create";

            var response = await _httpClient.PostAsJsonAsync(
                requestUri,
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<MoMoPaymentResponse>(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to create MoMo payment for order {OrderId}", request.OrderId);
            throw;
        }
    }

    /// <summary>
    /// Queries the status of a transaction from MoMo.
    /// </summary>
    public async Task<MoMoQueryResponse?> QueryTransactionAsync(
        MoMoQueryRequest request,
        string? apiEndpoint = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use provided apiEndpoint for per-tenant configuration, or fall back to HttpClient.BaseAddress
            var requestUri = string.IsNullOrEmpty(apiEndpoint)
                ? "query"
                : $"{apiEndpoint.TrimEnd('/')}/query";

            var response = await _httpClient.PostAsJsonAsync(
                requestUri,
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<MoMoQueryResponse>(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to query MoMo transaction {OrderId}", request.OrderId);
            throw;
        }
    }

    /// <summary>
    /// Requests a refund from MoMo.
    /// </summary>
    public async Task<MoMoRefundResponse?> RefundAsync(
        MoMoRefundRequest request,
        string? apiEndpoint = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use provided apiEndpoint for per-tenant configuration, or fall back to HttpClient.BaseAddress
            var requestUri = string.IsNullOrEmpty(apiEndpoint)
                ? "refund"
                : $"{apiEndpoint.TrimEnd('/')}/refund";

            var response = await _httpClient.PostAsJsonAsync(
                requestUri,
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<MoMoRefundResponse>(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to process MoMo refund for order {OrderId}", request.OrderId);
            throw;
        }
    }
}

/// <summary>
/// Interface for MoMo HTTP client.
/// </summary>
public interface IMoMoClient
{
    Task<MoMoPaymentResponse?> CreatePaymentAsync(MoMoPaymentRequest request, string? apiEndpoint = null, CancellationToken cancellationToken = default);
    Task<MoMoQueryResponse?> QueryTransactionAsync(MoMoQueryRequest request, string? apiEndpoint = null, CancellationToken cancellationToken = default);
    Task<MoMoRefundResponse?> RefundAsync(MoMoRefundRequest request, string? apiEndpoint = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for MoMo payment creation.
/// </summary>
public class MoMoPaymentRequest
{
    [JsonPropertyName("partnerCode")]
    public string PartnerCode { get; set; } = string.Empty;

    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("orderInfo")]
    public string OrderInfo { get; set; } = string.Empty;

    [JsonPropertyName("redirectUrl")]
    public string RedirectUrl { get; set; } = string.Empty;

    [JsonPropertyName("ipnUrl")]
    public string IpnUrl { get; set; } = string.Empty;

    [JsonPropertyName("requestType")]
    public string RequestType { get; set; } = "captureWallet";

    [JsonPropertyName("extraData")]
    public string ExtraData { get; set; } = string.Empty;

    [JsonPropertyName("lang")]
    public string Lang { get; set; } = "vi";

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;
}

/// <summary>
/// Response model for MoMo payment creation.
/// </summary>
public class MoMoPaymentResponse
{
    [JsonPropertyName("partnerCode")]
    public string? PartnerCode { get; set; }

    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("responseTime")]
    public long ResponseTime { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("resultCode")]
    public int ResultCode { get; set; }

    [JsonPropertyName("payUrl")]
    public string? PayUrl { get; set; }

    [JsonPropertyName("deeplink")]
    public string? Deeplink { get; set; }

    [JsonPropertyName("qrCodeUrl")]
    public string? QrCodeUrl { get; set; }
}

/// <summary>
/// Request model for MoMo transaction query.
/// </summary>
public class MoMoQueryRequest
{
    [JsonPropertyName("partnerCode")]
    public string PartnerCode { get; set; } = string.Empty;

    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("lang")]
    public string Lang { get; set; } = "vi";

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;
}

/// <summary>
/// Response model for MoMo transaction query.
/// </summary>
public class MoMoQueryResponse
{
    [JsonPropertyName("partnerCode")]
    public string? PartnerCode { get; set; }

    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("transId")]
    public long TransId { get; set; }

    [JsonPropertyName("resultCode")]
    public int ResultCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("payType")]
    public string? PayType { get; set; }

    [JsonPropertyName("responseTime")]
    public long ResponseTime { get; set; }

    [JsonPropertyName("extraData")]
    public string? ExtraData { get; set; }
}

/// <summary>
/// Request model for MoMo refund.
/// </summary>
public class MoMoRefundRequest
{
    [JsonPropertyName("partnerCode")]
    public string PartnerCode { get; set; } = string.Empty;

    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("transId")]
    public long TransId { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("lang")]
    public string Lang { get; set; } = "vi";

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;
}

/// <summary>
/// Response model for MoMo refund.
/// </summary>
public class MoMoRefundResponse
{
    [JsonPropertyName("partnerCode")]
    public string? PartnerCode { get; set; }

    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("transId")]
    public long TransId { get; set; }

    [JsonPropertyName("resultCode")]
    public int ResultCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("responseTime")]
    public long ResponseTime { get; set; }
}

/// <summary>
/// Model for MoMo IPN/Callback notification.
/// </summary>
public class MoMoCallbackData
{
    [JsonPropertyName("partnerCode")]
    public string? PartnerCode { get; set; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("orderInfo")]
    public string? OrderInfo { get; set; }

    [JsonPropertyName("orderType")]
    public string? OrderType { get; set; }

    [JsonPropertyName("transId")]
    public long TransId { get; set; }

    [JsonPropertyName("resultCode")]
    public int ResultCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("payType")]
    public string? PayType { get; set; }

    [JsonPropertyName("responseTime")]
    public long ResponseTime { get; set; }

    [JsonPropertyName("extraData")]
    public string? ExtraData { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}
