namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for currency conversion and exchange rate management.
/// </summary>
public interface ICurrencyService
{
    /// <summary>
    /// Convert an amount from one currency to another.
    /// </summary>
    /// <param name="amount">The amount to convert.</param>
    /// <param name="fromCurrency">Source currency code (e.g., "USD").</param>
    /// <param name="toCurrency">Target currency code (e.g., "VND").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The converted amount.</returns>
    Task<decimal> ConvertAsync(
        decimal amount,
        string fromCurrency,
        string toCurrency,
        CancellationToken ct = default);

    /// <summary>
    /// Get the exchange rate between two currencies.
    /// </summary>
    /// <param name="fromCurrency">Source currency code.</param>
    /// <param name="toCurrency">Target currency code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The exchange rate.</returns>
    Task<ExchangeRate> GetRateAsync(
        string fromCurrency,
        string toCurrency,
        CancellationToken ct = default);

    /// <summary>
    /// Get all supported currencies.
    /// </summary>
    IReadOnlyList<string> GetSupportedCurrencies();

    /// <summary>
    /// Check if a currency is supported.
    /// </summary>
    bool IsCurrencySupported(string currencyCode);
}

/// <summary>
/// Represents an exchange rate between two currencies.
/// </summary>
public record ExchangeRate(
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateTimeOffset FetchedAt);
