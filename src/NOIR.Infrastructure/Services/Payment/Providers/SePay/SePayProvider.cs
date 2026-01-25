using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NOIR.Application.Common.Interfaces;
using NOIR.Domain.Enums;

namespace NOIR.Infrastructure.Services.Payment.Providers.SePay;

/// <summary>
/// SePay payment gateway provider implementation.
/// Uses VietQR for bank transfers with webhook-based payment confirmation.
///
/// Payment Flow:
/// 1. Customer initiates payment → Generate VietQR code with unique reference
/// 2. Customer scans QR and transfers money from their bank app
/// 3. SePay detects the bank transfer (monitors account)
/// 4. SePay sends webhook to merchant with transaction details
/// 5. Merchant validates webhook and updates payment status
///
/// Key differences from redirect-based gateways:
/// - No redirect URL flow - customer gets QR code to scan
/// - Payment confirmed via webhook only (not return URL)
/// - No refund API - refunds handled manually or via bank transfer
/// </summary>
public class SePayProvider : IPaymentGatewayProvider
{
    private readonly ISePayClient _client;
    private readonly ILogger<SePayProvider> _logger;
    private readonly SePaySettings _defaultSettings;

    // Per-tenant credentials (populated via InitializeAsync)
    private string _apiToken = string.Empty;
    private string _bankAccountNumber = string.Empty;
    private string _bankCode = string.Empty;
    private string? _webhookApiKey;
    private string _apiBaseUrl = "https://my.sepay.vn/userapi";
    private string _qrBaseUrl = "https://qr.sepay.vn/img";
    private string _qrTemplate = "compact2";
    private GatewayEnvironment _environment = GatewayEnvironment.Sandbox;

    public SePayProvider(
        ISePayClient client,
        IOptions<SePaySettings> settings,
        ILogger<SePayProvider> logger)
    {
        _client = client;
        _logger = logger;
        _defaultSettings = settings.Value;
    }

    public string ProviderName => "sepay";

    public bool SupportsCOD => false;

    public Task InitializeAsync(
        Dictionary<string, string> credentials,
        GatewayEnvironment environment,
        CancellationToken ct = default)
    {
        _environment = environment;

        // Use credentials from database or fall back to config
        _apiToken = credentials.GetValueOrDefault("ApiToken") ?? _defaultSettings.ApiToken;
        _bankAccountNumber = credentials.GetValueOrDefault("BankAccountNumber") ?? _defaultSettings.BankAccountNumber;
        _bankCode = credentials.GetValueOrDefault("BankCode") ?? _defaultSettings.BankCode;
        _webhookApiKey = credentials.GetValueOrDefault("WebhookApiKey") ?? _defaultSettings.WebhookApiKey;
        _qrTemplate = credentials.GetValueOrDefault("QrTemplate") ?? _defaultSettings.QrTemplate;

        // SePay uses same URLs for sandbox and production
        _apiBaseUrl = credentials.GetValueOrDefault("ApiBaseUrl") ?? _defaultSettings.ApiBaseUrl;
        _qrBaseUrl = credentials.GetValueOrDefault("QrBaseUrl") ?? _defaultSettings.QrBaseUrl;

        return Task.CompletedTask;
    }

    public Task<PaymentInitiationResult> InitiatePaymentAsync(
        PaymentInitiationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            // Validate required credentials
            if (string.IsNullOrEmpty(_bankAccountNumber) || string.IsNullOrEmpty(_bankCode))
            {
                return Task.FromResult(new PaymentInitiationResult(
                    Success: false,
                    GatewayTransactionId: null,
                    PaymentUrl: null,
                    RequiresAction: false,
                    ErrorMessage: "SePay bank account not configured"));
            }

            // Generate unique reference code for this payment
            // Format: <prefix>-<transactionNumber>-<timestamp>
            // This will appear in the bank transfer memo for matching
            var referenceCode = GenerateReferenceCode(request.TransactionNumber);

            // SePay amount is in VND (integer, no decimals)
            var amount = (long)Math.Round(request.Amount);

            // Build VietQR URL
            // Format: https://qr.sepay.vn/img?acc={account}&bank={bank}&amount={amount}&des={description}&template={template}
            var qrDescription = $"TT {referenceCode}";
            var qrUrl = BuildVietQrUrl(amount, qrDescription);

            _logger.LogInformation(
                "SePay payment initiated for transaction {TransactionNumber}, Amount: {Amount} VND, Reference: {Reference}",
                request.TransactionNumber,
                amount,
                referenceCode);

            // Return QR code URL - customer will scan this to pay
            // The payment URL is the QR image URL for display
            return Task.FromResult(new PaymentInitiationResult(
                Success: true,
                GatewayTransactionId: referenceCode, // Use reference code as gateway ID for webhook matching
                PaymentUrl: qrUrl,
                RequiresAction: true, // Customer needs to scan QR and transfer
                ErrorMessage: null,
                AdditionalData: new Dictionary<string, string>
                {
                    ["reference_code"] = referenceCode,
                    ["qr_url"] = qrUrl,
                    ["bank_code"] = _bankCode,
                    ["bank_account"] = _bankAccountNumber,
                    ["amount"] = amount.ToString(),
                    ["description"] = qrDescription,
                    ["payment_type"] = "vietqr" // Indicates QR code payment
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate SePay payment for {TransactionNumber}", request.TransactionNumber);

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
            // Query SePay API for transactions matching our reference
            var response = await _client.GetTransactionsAsync(
                _apiToken,
                _bankAccountNumber,
                transactionId: null, // Search by content matching
                ct);

            if (response == null || response.Status != 200)
            {
                return new PaymentStatusResult(
                    Success: false,
                    Status: PaymentStatus.Pending,
                    GatewayTransactionId: gatewayTransactionId,
                    ErrorMessage: "Failed to query SePay transactions");
            }

            // Find transaction with matching reference code in content
            var matchingTransaction = response.Transactions
                .FirstOrDefault(t => t.TransactionContent.Contains(gatewayTransactionId, StringComparison.OrdinalIgnoreCase) ||
                                    t.Code.Equals(gatewayTransactionId, StringComparison.OrdinalIgnoreCase));

            if (matchingTransaction == null)
            {
                // No matching transaction found - payment still pending
                return new PaymentStatusResult(
                    Success: true,
                    Status: PaymentStatus.Pending,
                    GatewayTransactionId: gatewayTransactionId,
                    AdditionalData: new Dictionary<string, string>
                    {
                        ["message"] = "No matching bank transfer found"
                    });
            }

            // Found matching transaction - payment is confirmed
            _logger.LogInformation(
                "SePay payment found for reference {Reference}, SePay ID: {SePayId}, Amount: {Amount}",
                gatewayTransactionId,
                matchingTransaction.Id,
                matchingTransaction.AmountIn);

            return new PaymentStatusResult(
                Success: true,
                Status: PaymentStatus.Paid,
                GatewayTransactionId: matchingTransaction.Id.ToString(),
                AdditionalData: new Dictionary<string, string>
                {
                    ["sepay_id"] = matchingTransaction.Id.ToString(),
                    ["amount"] = matchingTransaction.AmountIn.ToString(),
                    ["transaction_date"] = matchingTransaction.TransactionDate,
                    ["reference_number"] = matchingTransaction.ReferenceNumber
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query SePay status for {TransactionId}", gatewayTransactionId);

            return new PaymentStatusResult(
                Success: false,
                Status: PaymentStatus.Pending,
                GatewayTransactionId: gatewayTransactionId,
                ErrorMessage: ex.Message);
        }
    }

    public Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken ct = default)
    {
        // SePay does not support refund API
        // Refunds must be processed manually via bank transfer
        _logger.LogWarning(
            "SePay does not support automatic refunds. Transaction {TransactionId} must be refunded manually.",
            request.GatewayTransactionId);

        return Task.FromResult(new RefundResult(
            Success: false,
            GatewayRefundId: null,
            ErrorMessage: "SePay does not support automatic refunds. Please process refund manually via bank transfer."));
    }

    public Task<WebhookValidationResult> ValidateWebhookAsync(
        WebhookPayload payload,
        CancellationToken ct = default)
    {
        try
        {
            // Parse webhook JSON body
            var webhookData = JsonSerializer.Deserialize<SePayWebhookPayload>(
                payload.RawBody,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

            if (webhookData == null)
            {
                return Task.FromResult(new WebhookValidationResult(
                    IsValid: false,
                    GatewayTransactionId: null,
                    EventType: null,
                    PaymentStatus: null,
                    GatewayEventId: null,
                    ErrorMessage: "Invalid webhook payload format"));
            }

            // Validate webhook API key if configured
            if (!string.IsNullOrEmpty(_webhookApiKey))
            {
                var authHeader = payload.Headers.GetValueOrDefault("Authorization", string.Empty);
                var apiKeyHeader = payload.Headers.GetValueOrDefault("X-API-Key", string.Empty);

                if (!authHeader.Contains(_webhookApiKey) && !apiKeyHeader.Equals(_webhookApiKey))
                {
                    _logger.LogWarning("Invalid SePay webhook API key");
                    return Task.FromResult(new WebhookValidationResult(
                        IsValid: false,
                        GatewayTransactionId: null,
                        EventType: null,
                        PaymentStatus: null,
                        GatewayEventId: null,
                        ErrorMessage: "Invalid webhook authentication"));
                }
            }

            // Validate this is an incoming transfer (payment)
            if (webhookData.TransferType?.ToLower() != "in" || webhookData.TransferAmount <= 0)
            {
                _logger.LogInformation(
                    "SePay webhook is not an incoming payment: Type={Type}, Amount={Amount}",
                    webhookData.TransferType,
                    webhookData.TransferAmount);

                return Task.FromResult(new WebhookValidationResult(
                    IsValid: true, // Valid webhook, just not a payment
                    GatewayTransactionId: webhookData.Id.ToString(),
                    EventType: "transfer_out",
                    PaymentStatus: null,
                    GatewayEventId: webhookData.Id.ToString(),
                    ErrorMessage: null));
            }

            // Extract reference code from transaction content
            // The content contains our reference code from the QR payment description
            var referenceCode = ExtractReferenceCode(webhookData.Content ?? webhookData.Description);

            _logger.LogInformation(
                "SePay webhook validated: ID={Id}, Amount={Amount}, Reference={Reference}, Content={Content}",
                webhookData.Id,
                webhookData.TransferAmount,
                referenceCode,
                webhookData.Content);

            return Task.FromResult(new WebhookValidationResult(
                IsValid: true,
                GatewayTransactionId: webhookData.Id.ToString(),
                EventType: "payment.success",
                PaymentStatus: PaymentStatus.Paid,
                GatewayEventId: referenceCode ?? webhookData.Id.ToString(),
                ErrorMessage: null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate SePay webhook");

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
        // Verify configuration is present
        if (string.IsNullOrEmpty(_apiToken) ||
            string.IsNullOrEmpty(_bankAccountNumber) ||
            string.IsNullOrEmpty(_bankCode))
        {
            return GatewayHealthStatus.Unhealthy;
        }

        try
        {
            // Try to get account info to verify API connectivity
            var accountInfo = await _client.GetAccountInfoAsync(_apiToken, ct);

            if (accountInfo == null || accountInfo.Status != 200)
            {
                _logger.LogWarning("SePay health check failed: Could not retrieve account info");
                return GatewayHealthStatus.Degraded;
            }

            // Verify our bank account is in the list
            var hasAccount = accountInfo.Accounts.Any(a =>
                a.AccountNumber == _bankAccountNumber);

            if (!hasAccount)
            {
                _logger.LogWarning(
                    "SePay health check warning: Configured account {Account} not found in SePay",
                    _bankAccountNumber);
                return GatewayHealthStatus.Degraded;
            }

            return GatewayHealthStatus.Healthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SePay health check failed");
            return GatewayHealthStatus.Unhealthy;
        }
    }

    /// <summary>
    /// Builds VietQR image URL for the payment.
    /// </summary>
    private string BuildVietQrUrl(long amount, string description)
    {
        // VietQR URL format: https://qr.sepay.vn/img?acc={}&bank={}&amount={}&des={}&template={}
        var encodedDescription = Uri.EscapeDataString(description);

        return $"{_qrBaseUrl}?acc={_bankAccountNumber}&bank={_bankCode}&amount={amount}&des={encodedDescription}&template={_qrTemplate}";
    }

    /// <summary>
    /// Generates a unique reference code for payment tracking.
    /// This code will be embedded in the QR payment description.
    /// </summary>
    private static string GenerateReferenceCode(string transactionNumber)
    {
        // Create a short, unique reference that's easy to match
        // Format: SP{timestamp}{hash} where hash is derived from transaction number
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()[^6..]; // Last 6 digits
        var hash = ComputeShortHash(transactionNumber);

        return $"SP{timestamp}{hash}";
    }

    /// <summary>
    /// Computes a short hash from the input string.
    /// </summary>
    private static string ComputeShortHash(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(inputBytes);
        // Take first 4 bytes and convert to alphanumeric
        return Convert.ToHexString(hashBytes[..4]).ToUpper();
    }

    /// <summary>
    /// Extracts the reference code from transaction content/description.
    /// The reference code follows format: SP{timestamp}{hash}
    /// </summary>
    private static string? ExtractReferenceCode(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return null;
        }

        // Look for SP followed by digits and hex chars (our reference format)
        var index = content.IndexOf("SP", StringComparison.OrdinalIgnoreCase);
        if (index >= 0 && content.Length >= index + 14)
        {
            // Extract SP + 6 digits + 8 hex chars = 16 chars total
            return content.Substring(index, Math.Min(16, content.Length - index));
        }

        return null;
    }
}
