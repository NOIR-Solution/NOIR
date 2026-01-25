using System.Text.Json;
using Microsoft.Extensions.Options;
using NOIR.Application.Common.Interfaces;
using NOIR.Application.Common.Settings;
using NOIR.Domain.Enums;

namespace NOIR.Infrastructure.Services.Payment.Providers.ZaloPay;

/// <summary>
/// ZaloPay payment gateway provider implementation.
/// Supports ZaloPay Wallet, ATM, Credit Card, and QR payments.
/// </summary>
public class ZaloPayProvider : IPaymentGatewayProvider
{
    private readonly IZaloPayClient _client;
    private readonly ILogger<ZaloPayProvider> _logger;
    private readonly ZaloPaySettings _defaultSettings;
    private readonly PaymentSettings _paymentSettings;

    // Per-tenant credentials (populated via InitializeAsync)
    private string _appId = string.Empty;
    private string _key1 = string.Empty;
    private string _key2 = string.Empty;
    private string _endpoint = string.Empty;
    private GatewayEnvironment _environment = GatewayEnvironment.Sandbox;

    public ZaloPayProvider(
        IZaloPayClient client,
        IOptions<ZaloPaySettings> settings,
        IOptions<PaymentSettings> paymentSettings,
        ILogger<ZaloPayProvider> logger)
    {
        _client = client;
        _logger = logger;
        _defaultSettings = settings.Value;
        _paymentSettings = paymentSettings.Value;
    }

    public string ProviderName => "zalopay";

    public bool SupportsCOD => false;

    public Task InitializeAsync(
        Dictionary<string, string> credentials,
        GatewayEnvironment environment,
        CancellationToken ct = default)
    {
        _environment = environment;

        // Use credentials from database or fall back to config
        _appId = credentials.GetValueOrDefault("AppId", _defaultSettings.AppId);
        _key1 = credentials.GetValueOrDefault("Key1", _defaultSettings.Key1);
        _key2 = credentials.GetValueOrDefault("Key2", _defaultSettings.Key2);

        // Set API endpoint based on environment
        if (environment == GatewayEnvironment.Production)
        {
            _endpoint = credentials.GetValueOrDefault("Endpoint", "https://openapi.zalopay.vn/v2");
        }
        else
        {
            _endpoint = credentials.GetValueOrDefault("Endpoint", _defaultSettings.Endpoint);
        }

        return Task.CompletedTask;
    }

    public async Task<PaymentInitiationResult> InitiatePaymentAsync(
        PaymentInitiationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            // ZaloPay amount is in VND (no decimal)
            var amount = (long)request.Amount;
            var appTransId = ZaloPaySignatureHelper.GenerateAppTransId(request.TransactionNumber);
            var appTime = ZaloPaySignatureHelper.GetTimestamp();

            var embedData = JsonSerializer.Serialize(new
            {
                redirecturl = request.ReturnUrl,
                promotioninfo = request.Metadata?.GetValueOrDefault("promotioninfo", string.Empty) ?? string.Empty
            });

            var item = JsonSerializer.Serialize(new[]
            {
                new
                {
                    name = $"Payment for {request.TransactionNumber}",
                    amount = amount,
                    quantity = 1
                }
            });

            var callbackUrl = $"{_paymentSettings.WebhookBaseUrl}/api/webhooks/zalopay";

            // Build MAC
            var macData = ZaloPaySignatureHelper.BuildOrderMacData(
                appId: _appId,
                appTransId: appTransId,
                appUser: "user",
                amount: amount,
                appTime: appTime,
                embedData: embedData,
                item: item);

            var mac = ZaloPaySignatureHelper.CreateMac(macData, _key1);

            var orderRequest = new ZaloPayOrderRequest
            {
                AppId = _appId,
                AppUser = "user",
                AppTransId = appTransId,
                AppTime = appTime,
                Amount = amount,
                Item = item,
                Description = $"Payment for order {request.TransactionNumber}",
                EmbedData = embedData,
                BankCode = _defaultSettings.BankCode,
                CallbackUrl = callbackUrl,
                Mac = mac
            };

            var response = await _client.CreateOrderAsync(orderRequest, _endpoint, ct);

            if (response == null || response.ReturnCode != 1)
            {
                _logger.LogWarning(
                    "ZaloPay order creation failed for {TransactionNumber}: {Message}",
                    request.TransactionNumber,
                    response?.ReturnMessage ?? "No response");

                return new PaymentInitiationResult(
                    Success: false,
                    GatewayTransactionId: null,
                    PaymentUrl: null,
                    RequiresAction: false,
                    ErrorMessage: response?.ReturnMessage ?? "Failed to create ZaloPay order");
            }

            _logger.LogInformation(
                "ZaloPay order created for transaction {TransactionNumber}, Amount: {Amount} VND",
                request.TransactionNumber,
                amount);

            return new PaymentInitiationResult(
                Success: true,
                GatewayTransactionId: appTransId,
                PaymentUrl: response.OrderUrl,
                RequiresAction: true,
                ErrorMessage: null,
                AdditionalData: new Dictionary<string, string>
                {
                    ["app_trans_id"] = appTransId,
                    ["zp_trans_token"] = response.ZpTransToken ?? string.Empty,
                    ["order_token"] = response.OrderToken ?? string.Empty,
                    ["qr_code"] = response.QrCode ?? string.Empty
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate ZaloPay payment for {TransactionNumber}", request.TransactionNumber);

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
            var macData = ZaloPaySignatureHelper.BuildQueryMacData(_appId, gatewayTransactionId, _key1);
            var mac = ZaloPaySignatureHelper.CreateMac(macData, _key1);

            var queryRequest = new ZaloPayQueryRequest
            {
                AppId = _appId,
                AppTransId = gatewayTransactionId,
                Mac = mac
            };

            var response = await _client.QueryOrderAsync(queryRequest, _endpoint, ct);

            if (response == null)
            {
                return new PaymentStatusResult(
                    Success: false,
                    Status: PaymentStatus.Pending,
                    GatewayTransactionId: gatewayTransactionId,
                    ErrorMessage: "No response from ZaloPay");
            }

            var status = MapReturnCodeToStatus(response.ReturnCode, response.IsProcessing);

            return new PaymentStatusResult(
                Success: true,
                Status: status,
                GatewayTransactionId: response.ZpTransId.ToString(),
                AdditionalData: new Dictionary<string, string>
                {
                    ["return_code"] = response.ReturnCode.ToString(),
                    ["return_message"] = response.ReturnMessage ?? string.Empty,
                    ["is_processing"] = response.IsProcessing.ToString(),
                    ["amount"] = response.Amount.ToString()
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query ZaloPay status for {TransactionId}", gatewayTransactionId);

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
            var amount = (long)request.Amount;
            var timestamp = ZaloPaySignatureHelper.GetTimestamp();

            var macData = ZaloPaySignatureHelper.BuildRefundMacData(
                appId: _appId,
                zpTransId: request.GatewayTransactionId,
                amount: amount,
                description: request.Reason ?? "Refund",
                timestamp: timestamp);

            var mac = ZaloPaySignatureHelper.CreateMac(macData, _key1);

            var refundRequest = new ZaloPayRefundRequest
            {
                AppId = _appId,
                ZpTransId = request.GatewayTransactionId,
                Amount = amount,
                Description = request.Reason ?? "Refund",
                Timestamp = timestamp,
                Mac = mac
            };

            var response = await _client.RefundAsync(refundRequest, _endpoint, ct);

            if (response == null || response.ReturnCode != 1)
            {
                return new RefundResult(
                    Success: false,
                    GatewayRefundId: null,
                    ErrorMessage: response?.ReturnMessage ?? "Refund failed");
            }

            _logger.LogInformation(
                "ZaloPay refund processed for transaction {TransactionId}, RefundId: {RefundId}",
                request.GatewayTransactionId,
                response.RefundId);

            return new RefundResult(
                Success: true,
                GatewayRefundId: response.RefundId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process ZaloPay refund for {TransactionId}", request.GatewayTransactionId);

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
            var callbackData = JsonSerializer.Deserialize<ZaloPayCallbackData>(payload.RawBody);

            if (callbackData == null || string.IsNullOrEmpty(callbackData.Data))
            {
                return Task.FromResult(new WebhookValidationResult(
                    IsValid: false,
                    GatewayTransactionId: null,
                    EventType: null,
                    PaymentStatus: null,
                    GatewayEventId: null,
                    ErrorMessage: "Failed to parse callback data"));
            }

            // Verify MAC using Key2
            var expectedMac = ZaloPaySignatureHelper.CreateMac(callbackData.Data, _key2);
            if (!string.Equals(expectedMac, callbackData.Mac, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid ZaloPay webhook MAC");

                return Task.FromResult(new WebhookValidationResult(
                    IsValid: false,
                    GatewayTransactionId: null,
                    EventType: null,
                    PaymentStatus: null,
                    GatewayEventId: null,
                    ErrorMessage: "Invalid MAC"));
            }

            // Parse the data payload
            var callbackPayload = JsonSerializer.Deserialize<ZaloPayCallbackPayload>(callbackData.Data);

            if (callbackPayload == null)
            {
                return Task.FromResult(new WebhookValidationResult(
                    IsValid: false,
                    GatewayTransactionId: null,
                    EventType: null,
                    PaymentStatus: null,
                    GatewayEventId: null,
                    ErrorMessage: "Failed to parse callback payload"));
            }

            // ZaloPay callback type: 1 = payment, 2 = refund
            var eventType = callbackData.Type == 1 ? "payment" : "refund";
            var status = callbackData.Type == 1 ? PaymentStatus.Paid : PaymentStatus.Refunded;

            _logger.LogInformation(
                "ZaloPay webhook validated for order {AppTransId}, Type: {Type}",
                callbackPayload.AppTransId,
                eventType);

            return Task.FromResult(new WebhookValidationResult(
                IsValid: true,
                GatewayTransactionId: callbackPayload.ZpTransId.ToString(),
                EventType: eventType,
                PaymentStatus: status,
                GatewayEventId: callbackPayload.AppTransId,
                ErrorMessage: null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate ZaloPay webhook");

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
        // Verify configuration is present
        if (string.IsNullOrEmpty(_appId) ||
            string.IsNullOrEmpty(_key1) ||
            string.IsNullOrEmpty(_key2))
        {
            return Task.FromResult(GatewayHealthStatus.Unhealthy);
        }

        return Task.FromResult(GatewayHealthStatus.Healthy);
    }

    /// <summary>
    /// Maps ZaloPay return code to internal payment status.
    /// </summary>
    private static PaymentStatus MapReturnCodeToStatus(int returnCode, bool isProcessing)
    {
        if (isProcessing)
        {
            return PaymentStatus.Processing;
        }

        return returnCode switch
        {
            1 => PaymentStatus.Paid,       // Success
            2 => PaymentStatus.Failed,     // Failed
            3 => PaymentStatus.Pending,    // Pending (order created but not paid)
            -49 => PaymentStatus.Expired,  // Expired
            _ => PaymentStatus.Failed
        };
    }
}
