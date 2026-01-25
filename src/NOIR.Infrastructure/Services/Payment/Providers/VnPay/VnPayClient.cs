using System.Net.Http.Json;
using System.Text.Json;

namespace NOIR.Infrastructure.Services.Payment.Providers.VnPay;

/// <summary>
/// HTTP client for VNPay API operations.
/// </summary>
public class VnPayClient : IVnPayClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VnPayClient> _logger;

    public VnPayClient(HttpClient httpClient, ILogger<VnPayClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Queries the status of a transaction from VNPay.
    /// </summary>
    public async Task<VnPayQueryResponse?> QueryTransactionAsync(
        VnPayQueryRequest request,
        string? apiUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use provided apiUrl for per-tenant configuration, or fall back to HttpClient.BaseAddress
            var requestUri = string.IsNullOrEmpty(apiUrl)
                ? "querydr"
                : $"{apiUrl.TrimEnd('/')}/querydr";

            var response = await _httpClient.PostAsJsonAsync(
                requestUri,
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<VnPayQueryResponse>(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to query VNPay transaction {TransactionId}", request.vnp_TxnRef);
            throw;
        }
    }

    /// <summary>
    /// Requests a refund from VNPay.
    /// </summary>
    public async Task<VnPayRefundResponse?> RefundAsync(
        VnPayRefundRequest request,
        string? apiUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use provided apiUrl for per-tenant configuration, or fall back to HttpClient.BaseAddress
            var requestUri = string.IsNullOrEmpty(apiUrl)
                ? "refund"
                : $"{apiUrl.TrimEnd('/')}/refund";

            var response = await _httpClient.PostAsJsonAsync(
                requestUri,
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<VnPayRefundResponse>(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to process VNPay refund for {TransactionId}", request.vnp_TxnRef);
            throw;
        }
    }
}

/// <summary>
/// Interface for VNPay HTTP client.
/// </summary>
public interface IVnPayClient
{
    Task<VnPayQueryResponse?> QueryTransactionAsync(VnPayQueryRequest request, string? apiUrl = null, CancellationToken cancellationToken = default);
    Task<VnPayRefundResponse?> RefundAsync(VnPayRefundRequest request, string? apiUrl = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for VNPay transaction query.
/// </summary>
public record VnPayQueryRequest(
    string vnp_RequestId,
    string vnp_Version,
    string vnp_Command,
    string vnp_TmnCode,
    string vnp_TxnRef,
    string vnp_OrderInfo,
    string vnp_TransactionNo,
    string vnp_TransactionDate,
    string vnp_CreateDate,
    string vnp_IpAddr,
    string vnp_SecureHash);

/// <summary>
/// Response model for VNPay transaction query.
/// </summary>
public record VnPayQueryResponse(
    string vnp_ResponseCode,
    string vnp_Message,
    string? vnp_TxnRef,
    decimal? vnp_Amount,
    string? vnp_OrderInfo,
    string? vnp_BankCode,
    string? vnp_PayDate,
    string? vnp_TransactionNo,
    string? vnp_TransactionType,
    string? vnp_TransactionStatus);

/// <summary>
/// Request model for VNPay refund.
/// </summary>
public record VnPayRefundRequest(
    string vnp_RequestId,
    string vnp_Version,
    string vnp_Command,
    string vnp_TmnCode,
    string vnp_TransactionType,
    string vnp_TxnRef,
    long vnp_Amount,
    string vnp_OrderInfo,
    string vnp_TransactionNo,
    string vnp_TransactionDate,
    string vnp_CreateBy,
    string vnp_CreateDate,
    string vnp_IpAddr,
    string vnp_SecureHash);

/// <summary>
/// Response model for VNPay refund.
/// </summary>
public record VnPayRefundResponse(
    string vnp_ResponseCode,
    string vnp_Message,
    string? vnp_TxnRef,
    decimal? vnp_Amount,
    string? vnp_TransactionNo,
    string? vnp_TransactionType,
    string? vnp_TransactionStatus);
