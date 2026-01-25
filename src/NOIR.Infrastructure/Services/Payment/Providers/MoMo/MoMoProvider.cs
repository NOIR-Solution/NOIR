using System.Text.Json;
using Microsoft.Extensions.Options;
using NOIR.Application.Common.Interfaces;
using NOIR.Application.Common.Settings;
using NOIR.Domain.Enums;

namespace NOIR.Infrastructure.Services.Payment.Providers.MoMo;

/// <summary>
/// MoMo payment gateway provider implementation.
/// Supports MoMo Wallet, QR Code, ATM, and Credit Card payments.
/// </summary>
public class MoMoProvider : IPaymentGatewayProvider
{
    private readonly IMoMoClient _client;
    private readonly ILogger<MoMoProvider> _logger;
    private readonly MoMoSettings _defaultSettings;
    private readonly PaymentSettings _paymentSettings;

    // Per-tenant credentials (populated via InitializeAsync)
    private string _partnerCode = string.Empty;
    private string _accessKey = string.Empty;
    private string _secretKey = string.Empty;
    private string _apiEndpoint = string.Empty;
    private string _requestType = "captureWallet";
    private GatewayEnvironment _environment = GatewayEnvironment.Sandbox;

    public MoMoProvider(
        IMoMoClient client,
        IOptions<MoMoSettings> settings,
        IOptions<PaymentSettings> paymentSettings,
        ILogger<MoMoProvider> logger)
    {
        _client = client;
        _logger = logger;
        _defaultSettings = settings.Value;
        _paymentSettings = paymentSettings.Value;
    }

    public string ProviderName => "momo";

    public bool SupportsCOD => false;

    public Task InitializeAsync(
        Dictionary<string, string> credentials,
        GatewayEnvironment environment,
        CancellationToken ct = default)
    {
        _environment = environment;

        // Use credentials from database or fall back to config
        _partnerCode = credentials.GetValueOrDefault("PartnerCode", _defaultSettings.PartnerCode);
        _accessKey = credentials.GetValueOrDefault("AccessKey", _defaultSettings.AccessKey);
        _secretKey = credentials.GetValueOrDefault("SecretKey", _defaultSettings.SecretKey);
        _requestType = credentials.GetValueOrDefault("RequestType", _defaultSettings.RequestType);

        // Set API endpoint based on environment
        if (environment == GatewayEnvironment.Production)
        {
            _apiEndpoint = credentials.GetValueOrDefault("ApiEndpoint", "https://payment.momo.vn/v2/gateway/api");
        }
        else
        {
            _apiEndpoint = credentials.GetValueOrDefault("ApiEndpoint", _defaultSettings.ApiEndpoint);
        }

        return Task.CompletedTask;
    }

    public async Task<PaymentInitiationResult> InitiatePaymentAsync(
        PaymentInitiationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            // MoMo amount is in VND (no decimal)
            var amount = (long)request.Amount;
            var requestId = Guid.NewGuid().ToString("N");
            var extraData = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(request.Metadata ?? new Dictionary<string, string>())));

            var ipnUrl = $"{_paymentSettings.WebhookBaseUrl}/api/webhooks/momo";

            // Build signature
            var signatureData = MoMoSignatureHelper.BuildPaymentSignatureData(
                accessKey: _accessKey,
                amount: amount,
                extraData: extraData,
                ipnUrl: ipnUrl,
                orderId: request.TransactionNumber,
                orderInfo: $"Payment for order {request.TransactionNumber}",
                partnerCode: _partnerCode,
                redirectUrl: request.ReturnUrl,
                requestId: requestId,
                requestType: _requestType);

            var signature = MoMoSignatureHelper.CreateSignature(signatureData, _secretKey);

            var paymentRequest = new MoMoPaymentRequest
            {
                PartnerCode = _partnerCode,
                RequestId = requestId,
                Amount = amount,
                OrderId = request.TransactionNumber,
                OrderInfo = $"Payment for order {request.TransactionNumber}",
                RedirectUrl = request.ReturnUrl,
                IpnUrl = ipnUrl,
                RequestType = _requestType,
                ExtraData = extraData,
                Lang = _defaultSettings.Lang,
                Signature = signature
            };

            var response = await _client.CreatePaymentAsync(paymentRequest, _apiEndpoint, ct);

            if (response == null || response.ResultCode != 0)
            {
                _logger.LogWarning(
                    "MoMo payment creation failed for {TransactionNumber}: {Message}",
                    request.TransactionNumber,
                    response?.Message ?? "No response");

                return new PaymentInitiationResult(
                    Success: false,
                    GatewayTransactionId: null,
                    PaymentUrl: null,
                    RequiresAction: false,
                    ErrorMessage: response?.Message ?? "Failed to create MoMo payment");
            }

            _logger.LogInformation(
                "MoMo payment initiated for transaction {TransactionNumber}, Amount: {Amount} VND",
                request.TransactionNumber,
                amount);

            return new PaymentInitiationResult(
                Success: true,
                GatewayTransactionId: response.OrderId,
                PaymentUrl: response.PayUrl,
                RequiresAction: true,
                ErrorMessage: null,
                AdditionalData: new Dictionary<string, string>
                {
                    ["deeplink"] = response.Deeplink ?? string.Empty,
                    ["qrCodeUrl"] = response.QrCodeUrl ?? string.Empty,
                    ["requestId"] = requestId
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate MoMo payment for {TransactionNumber}", request.TransactionNumber);

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
            var requestId = Guid.NewGuid().ToString("N");

            var signatureData = MoMoSignatureHelper.BuildQuerySignatureData(
                accessKey: _accessKey,
                orderId: gatewayTransactionId,
                partnerCode: _partnerCode,
                requestId: requestId);

            var signature = MoMoSignatureHelper.CreateSignature(signatureData, _secretKey);

            var queryRequest = new MoMoQueryRequest
            {
                PartnerCode = _partnerCode,
                RequestId = requestId,
                OrderId = gatewayTransactionId,
                Lang = _defaultSettings.Lang,
                Signature = signature
            };

            var response = await _client.QueryTransactionAsync(queryRequest, _apiEndpoint, ct);

            if (response == null)
            {
                return new PaymentStatusResult(
                    Success: false,
                    Status: PaymentStatus.Pending,
                    GatewayTransactionId: gatewayTransactionId,
                    ErrorMessage: "No response from MoMo");
            }

            var status = MapResultCodeToStatus(response.ResultCode);

            return new PaymentStatusResult(
                Success: true,
                Status: status,
                GatewayTransactionId: response.TransId.ToString(),
                AdditionalData: new Dictionary<string, string>
                {
                    ["resultCode"] = response.ResultCode.ToString(),
                    ["message"] = response.Message ?? string.Empty,
                    ["payType"] = response.PayType ?? string.Empty
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query MoMo status for {TransactionId}", gatewayTransactionId);

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
            var amount = (long)request.Amount;
            var transId = long.Parse(request.GatewayTransactionId);

            var signatureData = MoMoSignatureHelper.BuildRefundSignatureData(
                accessKey: _accessKey,
                amount: amount,
                description: request.Reason ?? "Refund",
                orderId: request.RefundNumber,
                partnerCode: _partnerCode,
                requestId: requestId,
                transId: transId);

            var signature = MoMoSignatureHelper.CreateSignature(signatureData, _secretKey);

            var refundRequest = new MoMoRefundRequest
            {
                PartnerCode = _partnerCode,
                RequestId = requestId,
                OrderId = request.RefundNumber,
                Amount = amount,
                TransId = transId,
                Description = request.Reason ?? "Refund",
                Lang = _defaultSettings.Lang,
                Signature = signature
            };

            var response = await _client.RefundAsync(refundRequest, _apiEndpoint, ct);

            if (response == null || response.ResultCode != 0)
            {
                return new RefundResult(
                    Success: false,
                    GatewayRefundId: null,
                    ErrorMessage: response?.Message ?? "Refund failed");
            }

            _logger.LogInformation(
                "MoMo refund processed for transaction {TransactionId}, RefundId: {RefundId}",
                request.GatewayTransactionId,
                response.TransId);

            return new RefundResult(
                Success: true,
                GatewayRefundId: response.TransId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process MoMo refund for {TransactionId}", request.GatewayTransactionId);

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
            var callbackData = JsonSerializer.Deserialize<MoMoCallbackData>(payload.RawBody);

            if (callbackData == null)
            {
                return Task.FromResult(new WebhookValidationResult(
                    IsValid: false,
                    GatewayTransactionId: null,
                    EventType: null,
                    PaymentStatus: null,
                    GatewayEventId: null,
                    ErrorMessage: "Failed to parse callback data"));
            }

            // Build signature for verification
            var signatureData = MoMoSignatureHelper.BuildCallbackSignatureData(
                accessKey: _accessKey,
                amount: callbackData.Amount,
                extraData: callbackData.ExtraData ?? string.Empty,
                message: callbackData.Message ?? string.Empty,
                orderId: callbackData.OrderId ?? string.Empty,
                orderInfo: callbackData.OrderInfo ?? string.Empty,
                orderType: callbackData.OrderType ?? string.Empty,
                partnerCode: callbackData.PartnerCode ?? string.Empty,
                payType: callbackData.PayType ?? string.Empty,
                requestId: callbackData.RequestId ?? string.Empty,
                responseTime: (int)callbackData.ResponseTime,
                resultCode: callbackData.ResultCode,
                transId: callbackData.TransId.ToString());

            if (!MoMoSignatureHelper.VerifySignature(signatureData, _secretKey, callbackData.Signature ?? string.Empty))
            {
                _logger.LogWarning("Invalid MoMo webhook signature for order {OrderId}", callbackData.OrderId);

                return Task.FromResult(new WebhookValidationResult(
                    IsValid: false,
                    GatewayTransactionId: null,
                    EventType: null,
                    PaymentStatus: null,
                    GatewayEventId: null,
                    ErrorMessage: "Invalid signature"));
            }

            var status = MapResultCodeToStatus(callbackData.ResultCode);

            _logger.LogInformation(
                "MoMo webhook validated for order {OrderId}, Status: {Status}",
                callbackData.OrderId,
                status);

            return Task.FromResult(new WebhookValidationResult(
                IsValid: true,
                GatewayTransactionId: callbackData.TransId.ToString(),
                EventType: "payment",
                PaymentStatus: status,
                GatewayEventId: callbackData.OrderId,
                ErrorMessage: null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate MoMo webhook");

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
        if (string.IsNullOrEmpty(_partnerCode) ||
            string.IsNullOrEmpty(_accessKey) ||
            string.IsNullOrEmpty(_secretKey))
        {
            return Task.FromResult(GatewayHealthStatus.Unhealthy);
        }

        return Task.FromResult(GatewayHealthStatus.Healthy);
    }

    /// <summary>
    /// Maps MoMo result code to internal payment status.
    /// </summary>
    private static PaymentStatus MapResultCodeToStatus(int resultCode)
    {
        return resultCode switch
        {
            0 => PaymentStatus.Paid,         // Success
            9000 => PaymentStatus.Paid,       // Transaction confirmed
            1000 => PaymentStatus.Processing, // Initiated
            1001 => PaymentStatus.Pending,    // User not paid yet
            1002 => PaymentStatus.Failed,     // Transaction rejected
            1003 => PaymentStatus.Cancelled,  // Transaction cancelled
            1004 => PaymentStatus.Failed,     // Amount mismatch
            1005 => PaymentStatus.Expired,    // Transaction expired
            1006 => PaymentStatus.Failed,     // User denied
            1007 => PaymentStatus.Refunded,   // Refunded
            _ => PaymentStatus.Failed         // Unknown error
        };
    }
}
