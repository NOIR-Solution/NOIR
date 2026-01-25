using System.Text;
using System.Text.Json;

namespace NOIR.Infrastructure.Services.Payment.Providers.PayOS;

/// <summary>
/// PayOS API client interface.
/// </summary>
public interface IPayOSClient
{
    /// <summary>
    /// Creates a payment link.
    /// </summary>
    Task<PayOSCreatePaymentResponse?> CreatePaymentLinkAsync(
        PayOSCreatePaymentRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets payment information by order code.
    /// </summary>
    Task<PayOSPaymentInfo?> GetPaymentInfoAsync(
        long orderCode,
        CancellationToken ct = default);

    /// <summary>
    /// Cancels a payment link.
    /// </summary>
    Task<PayOSCancelResponse?> CancelPaymentLinkAsync(
        long orderCode,
        string? reason = null,
        CancellationToken ct = default);
}

/// <summary>
/// PayOS API client implementation.
/// </summary>
public class PayOSClient : IPayOSClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PayOSClient> _logger;
    private readonly PayOSSettings _settings;

    private string _apiUrl = string.Empty;
    private string _clientId = string.Empty;
    private string _apiKey = string.Empty;
    private string _checksumKey = string.Empty;

    public PayOSClient(
        HttpClient httpClient,
        IOptions<PayOSSettings> settings,
        ILogger<PayOSClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;

        Initialize(_settings.ApiUrl, _settings.ClientId, _settings.ApiKey, _settings.ChecksumKey);
    }

    public void Initialize(string apiUrl, string clientId, string apiKey, string checksumKey)
    {
        _apiUrl = apiUrl;
        _clientId = clientId;
        _apiKey = apiKey;
        _checksumKey = checksumKey;
    }

    public async Task<PayOSCreatePaymentResponse?> CreatePaymentLinkAsync(
        PayOSCreatePaymentRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"{_apiUrl}/v2/payment-requests";

            // Create signature
            var signatureData = new SortedDictionary<string, string>
            {
                ["amount"] = request.Amount.ToString(),
                ["cancelUrl"] = request.CancelUrl,
                ["description"] = request.Description,
                ["orderCode"] = request.OrderCode.ToString(),
                ["returnUrl"] = request.ReturnUrl
            };

            request.Signature = PayOSSignatureHelper.CreateSignature(signatureData, _checksumKey);

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            httpRequest.Headers.Add("x-client-id", _clientId);
            httpRequest.Headers.Add("x-api-key", _apiKey);

            var response = await _httpClient.SendAsync(httpRequest, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayOS CreatePaymentLink failed: {StatusCode} - {Body}",
                    response.StatusCode, responseBody);
                return null;
            }

            return JsonSerializer.Deserialize<PayOSCreatePaymentResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create PayOS payment link");
            return null;
        }
    }

    public async Task<PayOSPaymentInfo?> GetPaymentInfoAsync(long orderCode, CancellationToken ct = default)
    {
        try
        {
            var url = $"{_apiUrl}/v2/payment-requests/{orderCode}";

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            httpRequest.Headers.Add("x-client-id", _clientId);
            httpRequest.Headers.Add("x-api-key", _apiKey);

            var response = await _httpClient.SendAsync(httpRequest, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayOS GetPaymentInfo failed: {StatusCode} - {Body}",
                    response.StatusCode, responseBody);
                return null;
            }

            var result = JsonSerializer.Deserialize<PayOSApiResponse<PayOSPaymentInfo>>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get PayOS payment info for order {OrderCode}", orderCode);
            return null;
        }
    }

    public async Task<PayOSCancelResponse?> CancelPaymentLinkAsync(
        long orderCode,
        string? reason = null,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"{_apiUrl}/v2/payment-requests/{orderCode}/cancel";

            var body = new { cancellationReason = reason ?? "Cancelled by user" };
            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            httpRequest.Headers.Add("x-client-id", _clientId);
            httpRequest.Headers.Add("x-api-key", _apiKey);

            var response = await _httpClient.SendAsync(httpRequest, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayOS CancelPaymentLink failed: {StatusCode} - {Body}",
                    response.StatusCode, responseBody);
                return null;
            }

            return JsonSerializer.Deserialize<PayOSCancelResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel PayOS payment for order {OrderCode}", orderCode);
            return null;
        }
    }
}

#region PayOS Models

public class PayOSCreatePaymentRequest
{
    public long OrderCode { get; set; }
    public long Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerEmail { get; set; }
    public string? BuyerPhone { get; set; }
    public string? BuyerAddress { get; set; }
    public List<PayOSItem>? Items { get; set; }
    public string CancelUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public int? ExpiredAt { get; set; }
    public string? Signature { get; set; }
}

public class PayOSItem
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public long Price { get; set; }
}

public class PayOSApiResponse<T>
{
    public string Code { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string? Signature { get; set; }
}

public class PayOSCreatePaymentResponse
{
    public string Code { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public PayOSPaymentData? Data { get; set; }
    public string? Signature { get; set; }
}

public class PayOSPaymentData
{
    public string Bin { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public long OrderCode { get; set; }
    public string Currency { get; set; } = "VND";
    public string PaymentLinkId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
}

public class PayOSPaymentInfo
{
    public string Id { get; set; } = string.Empty;
    public long OrderCode { get; set; }
    public long Amount { get; set; }
    public long AmountPaid { get; set; }
    public long AmountRemaining { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CreatedAt { get; set; }
    public List<PayOSTransaction>? Transactions { get; set; }
    public string? CancellationReason { get; set; }
    public string? CancelledAt { get; set; }
}

public class PayOSTransaction
{
    public string Reference { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TransactionDateTime { get; set; } = string.Empty;
    public string? VirtualAccountName { get; set; }
    public string? VirtualAccountNumber { get; set; }
    public string? CounterAccountBankId { get; set; }
    public string? CounterAccountBankName { get; set; }
    public string? CounterAccountName { get; set; }
    public string? CounterAccountNumber { get; set; }
}

public class PayOSCancelResponse
{
    public string Code { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public object? Data { get; set; }
}

public class PayOSWebhookData
{
    public long OrderCode { get; set; }
    public long Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string TransactionDateTime { get; set; } = string.Empty;
    public string Currency { get; set; } = "VND";
    public string PaymentLinkId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public string? CounterAccountBankId { get; set; }
    public string? CounterAccountBankName { get; set; }
    public string? CounterAccountName { get; set; }
    public string? CounterAccountNumber { get; set; }
    public string? VirtualAccountName { get; set; }
    public string? VirtualAccountNumber { get; set; }
}

#endregion
