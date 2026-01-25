using System.Web;
using Microsoft.Extensions.Options;
using NOIR.Application.Common.Interfaces;
using NOIR.Domain.Enums;

namespace NOIR.Infrastructure.Services.Payment.Providers.VnPay;

/// <summary>
/// VNPay payment gateway provider implementation.
/// Supports ATM, Internet Banking, QR Code, Credit/Debit Card payments.
/// </summary>
public class VnPayProvider : IPaymentGatewayProvider
{
    private readonly IVnPayClient _client;
    private readonly ILogger<VnPayProvider> _logger;
    private readonly VnPaySettings _defaultSettings;

    // Per-tenant credentials (populated via InitializeAsync)
    private string _tmnCode = string.Empty;
    private string _hashSecret = string.Empty;
    private string _paymentUrl = string.Empty;
    private string _apiUrl = string.Empty;
    private string _version = "2.1.0";
    private GatewayEnvironment _environment = GatewayEnvironment.Sandbox;

    public VnPayProvider(
        IVnPayClient client,
        IOptions<VnPaySettings> settings,
        ILogger<VnPayProvider> logger)
    {
        _client = client;
        _logger = logger;
        _defaultSettings = settings.Value;
    }

    public string ProviderName => "vnpay";

    public bool SupportsCOD => false;

    public Task InitializeAsync(
        Dictionary<string, string> credentials,
        GatewayEnvironment environment,
        CancellationToken ct = default)
    {
        _environment = environment;

        // Use credentials from database or fall back to config
        _tmnCode = credentials.GetValueOrDefault("TmnCode", _defaultSettings.TmnCode);
        _hashSecret = credentials.GetValueOrDefault("HashSecret", _defaultSettings.HashSecret);
        _version = credentials.GetValueOrDefault("Version", _defaultSettings.Version);

        // Set URLs based on environment
        if (environment == GatewayEnvironment.Production)
        {
            _paymentUrl = credentials.GetValueOrDefault("PaymentUrl", "https://pay.vnpay.vn/vpcpay.html");
            _apiUrl = credentials.GetValueOrDefault("ApiUrl", "https://merchant.vnpay.vn/merchant_webapi/api/transaction");
        }
        else
        {
            _paymentUrl = credentials.GetValueOrDefault("PaymentUrl", _defaultSettings.PaymentUrl);
            _apiUrl = credentials.GetValueOrDefault("ApiUrl", _defaultSettings.ApiUrl);
        }

        return Task.CompletedTask;
    }

    public Task<PaymentInitiationResult> InitiatePaymentAsync(
        PaymentInitiationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            // VNPay amount is in VND without decimal points (multiply by 100)
            var amount = (long)(request.Amount * 100);

            var parameters = new SortedDictionary<string, string>
            {
                ["vnp_Version"] = _version,
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = _tmnCode,
                ["vnp_Amount"] = amount.ToString(),
                ["vnp_CurrCode"] = request.Currency,
                ["vnp_TxnRef"] = request.TransactionNumber,
                ["vnp_OrderInfo"] = $"Payment for order {request.TransactionNumber}",
                ["vnp_OrderType"] = "other",
                ["vnp_Locale"] = _defaultSettings.Locale,
                ["vnp_ReturnUrl"] = request.ReturnUrl,
                ["vnp_IpAddr"] = request.Metadata?.GetValueOrDefault("IpAddress", "127.0.0.1") ?? "127.0.0.1",
                ["vnp_CreateDate"] = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"), // Vietnam time
                ["vnp_ExpireDate"] = DateTime.UtcNow.AddHours(7)
                    .AddMinutes(_defaultSettings.PaymentTimeoutMinutes)
                    .ToString("yyyyMMddHHmmss")
            };

            // Add bank code if specified
            if (request.Metadata?.TryGetValue("BankCode", out var bankCode) == true && !string.IsNullOrEmpty(bankCode))
            {
                parameters["vnp_BankCode"] = bankCode;
            }

            // Generate signature
            var dataString = VnPaySignatureHelper.BuildDataString(parameters);
            var signature = VnPaySignatureHelper.CreateSignature(dataString, _hashSecret);

            // Build payment URL
            var paymentUrl = $"{_paymentUrl}?{dataString}&vnp_SecureHash={signature}";

            _logger.LogInformation(
                "VNPay payment initiated for transaction {TransactionNumber}, Amount: {Amount} {Currency}",
                request.TransactionNumber,
                request.Amount,
                request.Currency);

            return Task.FromResult(new PaymentInitiationResult(
                Success: true,
                GatewayTransactionId: null, // VNPay assigns this after payment
                PaymentUrl: paymentUrl,
                RequiresAction: true,
                ErrorMessage: null,
                AdditionalData: new Dictionary<string, string>
                {
                    ["vnp_TxnRef"] = request.TransactionNumber
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate VNPay payment for {TransactionNumber}", request.TransactionNumber);

            return Task.FromResult(new PaymentInitiationResult(
                Success: false,
                GatewayTransactionId: null,
                PaymentUrl: null,
                RequiresAction: false,
                ErrorMessage: ex.Message));
        }
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(
        string gatewayTransactionId,
        CancellationToken ct = default)
    {
        try
        {
            var requestId = Guid.NewGuid().ToString("N");
            var createDate = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss");

            var parameters = new SortedDictionary<string, string>
            {
                ["vnp_RequestId"] = requestId,
                ["vnp_Version"] = _version,
                ["vnp_Command"] = "querydr",
                ["vnp_TmnCode"] = _tmnCode,
                ["vnp_TxnRef"] = gatewayTransactionId,
                ["vnp_OrderInfo"] = $"Query status for {gatewayTransactionId}",
                ["vnp_TransactionDate"] = createDate,
                ["vnp_CreateDate"] = createDate,
                ["vnp_IpAddr"] = "127.0.0.1"
            };

            var dataString = VnPaySignatureHelper.BuildDataString(parameters);
            var signature = VnPaySignatureHelper.CreateSignature(dataString, _hashSecret);

            var request = new VnPayQueryRequest(
                vnp_RequestId: requestId,
                vnp_Version: _version,
                vnp_Command: "querydr",
                vnp_TmnCode: _tmnCode,
                vnp_TxnRef: gatewayTransactionId,
                vnp_OrderInfo: $"Query status for {gatewayTransactionId}",
                vnp_TransactionNo: string.Empty,
                vnp_TransactionDate: createDate,
                vnp_CreateDate: createDate,
                vnp_IpAddr: "127.0.0.1",
                vnp_SecureHash: signature);

            var response = await _client.QueryTransactionAsync(request, _apiUrl, ct);

            if (response == null)
            {
                return new PaymentStatusResult(
                    Success: false,
                    Status: PaymentStatus.Pending,
                    GatewayTransactionId: gatewayTransactionId,
                    ErrorMessage: "No response from VNPay");
            }

            var status = MapResponseCodeToStatus(response.vnp_TransactionStatus ?? response.vnp_ResponseCode);

            return new PaymentStatusResult(
                Success: true,
                Status: status,
                GatewayTransactionId: response.vnp_TransactionNo,
                AdditionalData: new Dictionary<string, string>
                {
                    ["vnp_ResponseCode"] = response.vnp_ResponseCode,
                    ["vnp_Message"] = response.vnp_Message
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query VNPay status for {TransactionId}", gatewayTransactionId);

            return new PaymentStatusResult(
                Success: false,
                Status: PaymentStatus.Pending,
                GatewayTransactionId: gatewayTransactionId,
                ErrorMessage: ex.Message);
        }
    }

    public async Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken ct = default)
    {
        try
        {
            var requestId = Guid.NewGuid().ToString("N");
            var createDate = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss");
            var amount = (long)(request.Amount * 100);

            var parameters = new SortedDictionary<string, string>
            {
                ["vnp_RequestId"] = requestId,
                ["vnp_Version"] = _version,
                ["vnp_Command"] = "refund",
                ["vnp_TmnCode"] = _tmnCode,
                ["vnp_TransactionType"] = "02", // Full refund
                ["vnp_TxnRef"] = request.RefundNumber,
                ["vnp_Amount"] = amount.ToString(),
                ["vnp_OrderInfo"] = request.Reason ?? "Refund",
                ["vnp_TransactionNo"] = request.GatewayTransactionId,
                ["vnp_TransactionDate"] = createDate,
                ["vnp_CreateBy"] = "system",
                ["vnp_CreateDate"] = createDate,
                ["vnp_IpAddr"] = "127.0.0.1"
            };

            var dataString = VnPaySignatureHelper.BuildDataString(parameters);
            var signature = VnPaySignatureHelper.CreateSignature(dataString, _hashSecret);

            var refundRequest = new VnPayRefundRequest(
                vnp_RequestId: requestId,
                vnp_Version: _version,
                vnp_Command: "refund",
                vnp_TmnCode: _tmnCode,
                vnp_TransactionType: "02",
                vnp_TxnRef: request.RefundNumber,
                vnp_Amount: amount,
                vnp_OrderInfo: request.Reason ?? "Refund",
                vnp_TransactionNo: request.GatewayTransactionId,
                vnp_TransactionDate: createDate,
                vnp_CreateBy: "system",
                vnp_CreateDate: createDate,
                vnp_IpAddr: "127.0.0.1",
                vnp_SecureHash: signature);

            var response = await _client.RefundAsync(refundRequest, _apiUrl, ct);

            if (response == null || response.vnp_ResponseCode != "00")
            {
                return new RefundResult(
                    Success: false,
                    GatewayRefundId: null,
                    ErrorMessage: response?.vnp_Message ?? "Refund failed");
            }

            _logger.LogInformation(
                "VNPay refund processed for transaction {TransactionId}, RefundId: {RefundId}",
                request.GatewayTransactionId,
                response.vnp_TransactionNo);

            return new RefundResult(
                Success: true,
                GatewayRefundId: response.vnp_TransactionNo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process VNPay refund for {TransactionId}", request.GatewayTransactionId);

            return new RefundResult(
                Success: false,
                GatewayRefundId: null,
                ErrorMessage: ex.Message);
        }
    }

    public Task<WebhookValidationResult> ValidateWebhookAsync(
        WebhookPayload payload,
        CancellationToken ct = default)
    {
        try
        {
            var parameters = VnPaySignatureHelper.ParseQueryString(payload.RawBody);

            if (!VnPaySignatureHelper.ValidateResponseSignature(parameters, _hashSecret))
            {
                _logger.LogWarning("Invalid VNPay webhook signature");

                return Task.FromResult(new WebhookValidationResult(
                    IsValid: false,
                    GatewayTransactionId: null,
                    EventType: null,
                    PaymentStatus: null,
                    GatewayEventId: null,
                    ErrorMessage: "Invalid signature"));
            }

            var transactionRef = parameters.GetValueOrDefault("vnp_TxnRef");
            var transactionNo = parameters.GetValueOrDefault("vnp_TransactionNo");
            var responseCode = parameters.GetValueOrDefault("vnp_ResponseCode", "99");
            var transactionStatus = parameters.GetValueOrDefault("vnp_TransactionStatus", responseCode);

            var status = MapResponseCodeToStatus(transactionStatus);

            _logger.LogInformation(
                "VNPay webhook validated for transaction {TransactionRef}, Status: {Status}",
                transactionRef,
                status);

            return Task.FromResult(new WebhookValidationResult(
                IsValid: true,
                GatewayTransactionId: transactionNo,
                EventType: "payment",
                PaymentStatus: status,
                GatewayEventId: transactionRef,
                ErrorMessage: null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate VNPay webhook");

            return Task.FromResult(new WebhookValidationResult(
                IsValid: false,
                GatewayTransactionId: null,
                EventType: null,
                PaymentStatus: null,
                GatewayEventId: null,
                ErrorMessage: ex.Message));
        }
    }

    public Task<GatewayHealthStatus> HealthCheckAsync(CancellationToken ct = default)
    {
        // VNPay doesn't have a dedicated health check endpoint
        // We verify configuration is present as a basic check
        if (string.IsNullOrEmpty(_tmnCode) || string.IsNullOrEmpty(_hashSecret))
        {
            return Task.FromResult(GatewayHealthStatus.Unhealthy);
        }

        return Task.FromResult(GatewayHealthStatus.Healthy);
    }

    /// <summary>
    /// Maps VNPay response code to internal payment status.
    /// </summary>
    private static PaymentStatus MapResponseCodeToStatus(string responseCode)
    {
        return responseCode switch
        {
            "00" => PaymentStatus.Paid,
            "07" => PaymentStatus.Paid, // Suspicious transaction but successful
            "09" => PaymentStatus.Failed, // Card not registered
            "10" => PaymentStatus.Failed, // Authentication failed
            "11" => PaymentStatus.Expired, // Payment timeout
            "12" => PaymentStatus.Failed, // Card blocked
            "13" => PaymentStatus.Failed, // Wrong OTP
            "24" => PaymentStatus.Cancelled, // Customer cancelled
            "51" => PaymentStatus.Failed, // Insufficient balance
            "65" => PaymentStatus.Failed, // Exceeded limit
            "75" => PaymentStatus.Failed, // Bank maintenance
            "79" => PaymentStatus.Failed, // Wrong password
            "99" => PaymentStatus.Failed, // Other errors
            _ => PaymentStatus.Pending
        };
    }
}
