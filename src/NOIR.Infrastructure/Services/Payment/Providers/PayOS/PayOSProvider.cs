using System.Text.Json;

namespace NOIR.Infrastructure.Services.Payment.Providers.PayOS;

/// <summary>
/// PayOS payment gateway provider implementation.
/// Modern Vietnam gateway with QR payment, bank transfer, and excellent developer experience.
/// </summary>
public class PayOSProvider : IPaymentGatewayProvider
{
    private readonly IPayOSClient _client;
    private readonly ILogger<PayOSProvider> _logger;
    private readonly PayOSSettings _defaultSettings;

    // Per-tenant credentials (populated via InitializeAsync)
    private string _clientId = string.Empty;
    private string _apiKey = string.Empty;
    private string _checksumKey = string.Empty;
    private string _apiUrl = string.Empty;
    private GatewayEnvironment _environment = GatewayEnvironment.Sandbox;

    public PayOSProvider(
        IPayOSClient client,
        IOptions<PayOSSettings> settings,
        ILogger<PayOSProvider> logger)
    {
        _client = client;
        _logger = logger;
        _defaultSettings = settings.Value;
    }

    public string ProviderName => "payos";

    public bool SupportsCOD => false;

    public Task InitializeAsync(
        Dictionary<string, string> credentials,
        GatewayEnvironment environment,
        CancellationToken ct = default)
    {
        _environment = environment;

        // Use credentials from database or fall back to config
        _clientId = credentials.GetValueOrDefault("ClientId", _defaultSettings.ClientId);
        _apiKey = credentials.GetValueOrDefault("ApiKey", _defaultSettings.ApiKey);
        _checksumKey = credentials.GetValueOrDefault("ChecksumKey", _defaultSettings.ChecksumKey);
        _apiUrl = credentials.GetValueOrDefault("ApiUrl", _defaultSettings.ApiUrl);

        // Initialize client with credentials
        if (_client is PayOSClient payOSClient)
        {
            payOSClient.Initialize(_apiUrl, _clientId, _apiKey, _checksumKey);
        }

        return Task.CompletedTask;
    }

    public async Task<PaymentInitiationResult> InitiatePaymentAsync(
        PaymentInitiationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            // PayOS uses integer order codes
            var orderCode = GenerateOrderCode(request.TransactionNumber);

            var payOSRequest = new PayOSCreatePaymentRequest
            {
                OrderCode = orderCode,
                Amount = (long)request.Amount, // VND doesn't use decimals
                Description = $"Payment for {request.TransactionNumber}",
                CancelUrl = request.Metadata?.GetValueOrDefault("CancelUrl", request.ReturnUrl) ?? request.ReturnUrl,
                ReturnUrl = request.ReturnUrl,
                ExpiredAt = (int)(DateTimeOffset.UtcNow.AddMinutes(_defaultSettings.PaymentTimeoutMinutes).ToUnixTimeSeconds()),
                BuyerName = request.Metadata?.GetValueOrDefault("BuyerName", "Customer") ?? "Customer",
                BuyerEmail = request.Metadata?.GetValueOrDefault("BuyerEmail"),
                BuyerPhone = request.Metadata?.GetValueOrDefault("BuyerPhone")
            };

            var response = await _client.CreatePaymentLinkAsync(payOSRequest, ct);

            if (response == null || response.Code != "00" || response.Data == null)
            {
                _logger.LogError(
                    "PayOS CreatePaymentLink failed for {TransactionNumber}: {Code} - {Desc}",
                    request.TransactionNumber,
                    response?.Code,
                    response?.Desc);

                return new PaymentInitiationResult(
                    Success: false,
                    GatewayTransactionId: null,
                    PaymentUrl: null,
                    RequiresAction: false,
                    ErrorMessage: response?.Desc ?? "Failed to create payment link");
            }

            _logger.LogInformation(
                "PayOS payment created: {PaymentLinkId} for transaction {TransactionNumber}, Amount: {Amount} VND",
                response.Data.PaymentLinkId,
                request.TransactionNumber,
                request.Amount);

            return new PaymentInitiationResult(
                Success: true,
                GatewayTransactionId: response.Data.PaymentLinkId,
                PaymentUrl: response.Data.CheckoutUrl,
                RequiresAction: true,
                ErrorMessage: null,
                AdditionalData: new Dictionary<string, string>
                {
                    ["payment_link_id"] = response.Data.PaymentLinkId,
                    ["order_code"] = orderCode.ToString(),
                    ["qr_code"] = response.Data.QrCode,
                    ["account_number"] = response.Data.AccountNumber,
                    ["account_name"] = response.Data.AccountName,
                    ["bin"] = response.Data.Bin
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate PayOS payment for {TransactionNumber}", request.TransactionNumber);

            return new PaymentInitiationResult(
                Success: false,
                GatewayTransactionId: null,
                PaymentUrl: null,
                RequiresAction: false,
                ErrorMessage: ex.Message);
        }
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(
        string gatewayTransactionId,
        CancellationToken ct = default)
    {
        try
        {
            // gatewayTransactionId could be order code or payment link id
            if (!long.TryParse(gatewayTransactionId, out var orderCode))
            {
                return new PaymentStatusResult(
                    Success: false,
                    Status: PaymentStatus.Pending,
                    GatewayTransactionId: gatewayTransactionId,
                    ErrorMessage: "Invalid order code format");
            }

            var paymentInfo = await _client.GetPaymentInfoAsync(orderCode, ct);

            if (paymentInfo == null)
            {
                return new PaymentStatusResult(
                    Success: false,
                    Status: PaymentStatus.Pending,
                    GatewayTransactionId: gatewayTransactionId,
                    ErrorMessage: "Payment not found");
            }

            var status = MapPayOSStatusToPaymentStatus(paymentInfo.Status);

            return new PaymentStatusResult(
                Success: true,
                Status: status,
                GatewayTransactionId: paymentInfo.Id,
                AdditionalData: new Dictionary<string, string>
                {
                    ["payos_status"] = paymentInfo.Status,
                    ["amount"] = paymentInfo.Amount.ToString(),
                    ["amount_paid"] = paymentInfo.AmountPaid.ToString(),
                    ["amount_remaining"] = paymentInfo.AmountRemaining.ToString()
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get PayOS payment status for {TransactionId}", gatewayTransactionId);

            return new PaymentStatusResult(
                Success: false,
                Status: PaymentStatus.Pending,
                GatewayTransactionId: gatewayTransactionId,
                ErrorMessage: ex.Message);
        }
    }

    public Task<Application.Common.Interfaces.RefundResult> RefundAsync(
        Application.Common.Interfaces.RefundRequest request,
        CancellationToken ct = default)
    {
        // PayOS doesn't have a direct refund API - refunds are processed manually
        // through the PayOS dashboard or via bank transfer
        _logger.LogWarning(
            "PayOS refund requested for {TransactionId} - manual processing required",
            request.GatewayTransactionId);

        return Task.FromResult(new Application.Common.Interfaces.RefundResult(
            Success: false,
            GatewayRefundId: null,
            ErrorMessage: "PayOS refunds require manual processing through the dashboard"));
    }

    public Task<WebhookValidationResult> ValidateWebhookAsync(
        WebhookPayload payload,
        CancellationToken ct = default)
    {
        try
        {
            var webhookData = JsonSerializer.Deserialize<PayOSApiResponse<PayOSWebhookData>>(
                payload.RawBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (webhookData?.Data == null)
            {
                return Task.FromResult(new WebhookValidationResult(
                    IsValid: false,
                    GatewayTransactionId: null,
                    EventType: null,
                    PaymentStatus: null,
                    GatewayEventId: null,
                    ErrorMessage: "Invalid webhook payload"));
            }

            // Verify signature if present
            if (!string.IsNullOrEmpty(webhookData.Signature))
            {
                var signatureData = new SortedDictionary<string, string>
                {
                    ["orderCode"] = webhookData.Data.OrderCode.ToString(),
                    ["amount"] = webhookData.Data.Amount.ToString(),
                    ["description"] = webhookData.Data.Description,
                    ["accountNumber"] = webhookData.Data.AccountNumber,
                    ["reference"] = webhookData.Data.Reference,
                    ["transactionDateTime"] = webhookData.Data.TransactionDateTime,
                    ["currency"] = webhookData.Data.Currency,
                    ["paymentLinkId"] = webhookData.Data.PaymentLinkId,
                    ["code"] = webhookData.Data.Code,
                    ["desc"] = webhookData.Data.Desc
                };

                if (!PayOSSignatureHelper.VerifySignature(signatureData, webhookData.Signature, _checksumKey))
                {
                    _logger.LogWarning("Invalid PayOS webhook signature");

                    return Task.FromResult(new WebhookValidationResult(
                        IsValid: false,
                        GatewayTransactionId: null,
                        EventType: null,
                        PaymentStatus: null,
                        GatewayEventId: null,
                        ErrorMessage: "Invalid signature"));
                }
            }

            var status = MapPayOSCodeToPaymentStatus(webhookData.Data.Code);

            _logger.LogInformation(
                "PayOS webhook validated for order {OrderCode}, Status: {Status}",
                webhookData.Data.OrderCode,
                status);

            return Task.FromResult(new WebhookValidationResult(
                IsValid: true,
                GatewayTransactionId: webhookData.Data.PaymentLinkId,
                EventType: "payment",
                PaymentStatus: status,
                GatewayEventId: webhookData.Data.OrderCode.ToString()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate PayOS webhook");

            return Task.FromResult(new WebhookValidationResult(
                IsValid: false,
                GatewayTransactionId: null,
                EventType: null,
                PaymentStatus: null,
                GatewayEventId: null,
                ErrorMessage: ex.Message));
        }
    }

    public async Task<GatewayHealthStatus> HealthCheckAsync(CancellationToken ct = default)
    {
        // First check if credentials are configured
        if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_checksumKey))
        {
            _logger.LogWarning("PayOS health check failed: Missing credentials");
            return GatewayHealthStatus.Unhealthy;
        }

        try
        {
            // Create a test payment request to verify API connectivity
            // Use a minimal test request that will fail validation but prove API is accessible
            var testRequest = new PayOSCreatePaymentRequest
            {
                OrderCode = 0,
                Amount = 1000,
                Description = "Health check",
                CancelUrl = "https://test.local/cancel",
                ReturnUrl = "https://test.local/return"
            };

            var result = await _client.CreatePaymentLinkAsync(testRequest, ct);

            // Any response (even an error response like "invalid order code") means API is accessible
            // PayOS returns Code != "00" for validation errors, which is fine for health check
            if (result != null)
            {
                _logger.LogDebug("PayOS health check succeeded: API is accessible (Code: {Code})", result.Code);
                return GatewayHealthStatus.Healthy;
            }

            // Null result means HTTP request failed
            _logger.LogWarning("PayOS health check failed: No response from API");
            return GatewayHealthStatus.Unhealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PayOS health check failed");
            return GatewayHealthStatus.Unhealthy;
        }
    }

    /// <summary>
    /// Maps PayOS status to internal payment status.
    /// </summary>
    private static PaymentStatus MapPayOSStatusToPaymentStatus(string status)
    {
        return status.ToUpperInvariant() switch
        {
            "PAID" => PaymentStatus.Paid,
            "PENDING" => PaymentStatus.Pending,
            "PROCESSING" => PaymentStatus.Processing,
            "CANCELLED" => PaymentStatus.Cancelled,
            "EXPIRED" => PaymentStatus.Expired,
            _ => PaymentStatus.Pending
        };
    }

    /// <summary>
    /// Maps PayOS webhook code to payment status.
    /// </summary>
    private static PaymentStatus MapPayOSCodeToPaymentStatus(string code)
    {
        return code switch
        {
            "00" => PaymentStatus.Paid,
            "01" => PaymentStatus.Failed, // Invalid parameters
            "02" => PaymentStatus.Pending, // Processing
            _ => PaymentStatus.Failed
        };
    }

    /// <summary>
    /// Generates a unique numeric order code from transaction number.
    /// </summary>
    private static long GenerateOrderCode(string transactionNumber)
    {
        // Use timestamp + hash of transaction number for unique code
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 100000000;
        var hash = Math.Abs(transactionNumber.GetHashCode()) % 10000;
        return timestamp * 10000 + hash;
    }
}
