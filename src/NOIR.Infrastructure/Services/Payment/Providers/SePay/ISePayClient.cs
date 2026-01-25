namespace NOIR.Infrastructure.Services.Payment.Providers.SePay;

/// <summary>
/// HTTP client interface for SePay API operations.
/// </summary>
public interface ISePayClient
{
    /// <summary>
    /// Gets recent transactions from SePay.
    /// Used to verify payment status when webhook fails.
    /// </summary>
    Task<SePayTransactionListResponse?> GetTransactionsAsync(
        string apiToken,
        string? accountNumber = null,
        string? transactionId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets bank account balance information.
    /// Used for health checks.
    /// </summary>
    Task<SePayAccountResponse?> GetAccountInfoAsync(
        string apiToken,
        CancellationToken ct = default);
}

/// <summary>
/// Response from SePay transaction list API.
/// </summary>
public record SePayTransactionListResponse(
    int Status,
    SePayTransactionListMessages Messages,
    SePayTransaction[] Transactions);

public record SePayTransactionListMessages(string Success);

/// <summary>
/// Individual transaction from SePay.
/// </summary>
public record SePayTransaction(
    long Id,
    string Gateway,
    string TransactionDate,
    string AccountNumber,
    string? SubAccount,
    decimal AmountIn,
    decimal AmountOut,
    decimal Accumulated,
    string Code,
    string TransactionContent,
    string ReferenceNumber,
    string Body);

/// <summary>
/// Response from SePay account info API.
/// </summary>
public record SePayAccountResponse(
    int Status,
    SePayAccountMessages Messages,
    SePayAccount[] Accounts);

public record SePayAccountMessages(string Success);

public record SePayAccount(
    long Id,
    string Bank,
    string AccountNumber,
    string Label,
    decimal AccumulatedAmount,
    decimal Balance,
    string LastTransaction);

/// <summary>
/// SePay webhook payload structure.
/// Sent when a bank transfer is detected.
/// </summary>
public record SePayWebhookPayload(
    long Id,
    string Gateway,
    string TransactionDate,
    string AccountNumber,
    string Code,
    string Content,
    string TransferType,
    decimal TransferAmount,
    decimal Accumulated,
    string? SubAccount,
    string ReferenceCode,
    string Description);
