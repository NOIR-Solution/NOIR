using ZiggyCreatures.Caching.Fusion;

namespace NOIR.Infrastructure.Services.Payment;

/// <summary>
/// Currency conversion service with caching support.
/// Uses external API for exchange rates with FusionCache for performance.
/// </summary>
public class CurrencyService : ICurrencyService
{
    private readonly HttpClient _httpClient;
    private readonly IFusionCache _cache;
    private readonly CurrencySettings _settings;
    private readonly ILogger<CurrencyService> _logger;

    private static readonly IReadOnlyList<string> SupportedCurrencies =
    [
        "VND", "USD", "EUR", "GBP", "JPY", "CNY", "KRW", "SGD", "THB", "AUD", "CAD", "CHF"
    ];

    public CurrencyService(
        HttpClient httpClient,
        IFusionCache cache,
        IOptions<CurrencySettings> settings,
        ILogger<CurrencyService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<decimal> ConvertAsync(
        decimal amount,
        string fromCurrency,
        string toCurrency,
        CancellationToken ct = default)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return amount;
        }

        var rate = await GetRateAsync(fromCurrency, toCurrency, ct);
        var convertedAmount = amount * rate.Rate;

        _logger.LogDebug(
            "Converted {Amount} {From} to {ConvertedAmount} {To} (rate: {Rate})",
            amount, fromCurrency, convertedAmount, toCurrency, rate.Rate);

        return Math.Round(convertedAmount, 2);
    }

    public async Task<ExchangeRate> GetRateAsync(
        string fromCurrency,
        string toCurrency,
        CancellationToken ct = default)
    {
        fromCurrency = fromCurrency.ToUpperInvariant();
        toCurrency = toCurrency.ToUpperInvariant();

        if (!IsCurrencySupported(fromCurrency))
        {
            throw new ArgumentException($"Currency '{fromCurrency}' is not supported", nameof(fromCurrency));
        }

        if (!IsCurrencySupported(toCurrency))
        {
            throw new ArgumentException($"Currency '{toCurrency}' is not supported", nameof(toCurrency));
        }

        var cacheKey = $"exchange_rate:{fromCurrency}:{toCurrency}";

        return await _cache.GetOrSetAsync(
            cacheKey,
            async token => await FetchRateAsync(fromCurrency, toCurrency, token),
            options => options
                .SetDuration(TimeSpan.FromMinutes(_settings.RateCacheMinutes))
                .SetFailSafe(true, TimeSpan.FromHours(24)),
            ct);
    }

    public IReadOnlyList<string> GetSupportedCurrencies() => SupportedCurrencies;

    public bool IsCurrencySupported(string currencyCode)
    {
        return SupportedCurrencies.Contains(currencyCode.ToUpperInvariant());
    }

    private async Task<ExchangeRate> FetchRateAsync(
        string fromCurrency,
        string toCurrency,
        CancellationToken ct)
    {
        // Check for manual rate overrides first
        if (_settings.ManualRates.TryGetValue($"{fromCurrency}_{toCurrency}", out var manualRate))
        {
            _logger.LogDebug("Using manual rate for {From}/{To}: {Rate}", fromCurrency, toCurrency, manualRate);
            return new ExchangeRate(fromCurrency, toCurrency, manualRate, DateTimeOffset.UtcNow);
        }

        // Use external API if configured
        if (!string.IsNullOrEmpty(_settings.ExchangeRateApiKey))
        {
            try
            {
                var rate = await FetchFromApiAsync(fromCurrency, toCurrency, ct);
                return rate;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch exchange rate from API, using fallback rates");
            }
        }

        // Fallback to default rates
        var fallbackRate = GetFallbackRate(fromCurrency, toCurrency);
        return new ExchangeRate(fromCurrency, toCurrency, fallbackRate, DateTimeOffset.UtcNow);
    }

    private async Task<ExchangeRate> FetchFromApiAsync(
        string fromCurrency,
        string toCurrency,
        CancellationToken ct)
    {
        // Using exchangerate-api.com format
        var url = $"{_settings.ExchangeRateApiUrl}/{_settings.ExchangeRateApiKey}/pair/{fromCurrency}/{toCurrency}";

        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        var json = JsonDocument.Parse(content);

        if (json.RootElement.TryGetProperty("conversion_rate", out var rateElement))
        {
            var rate = rateElement.GetDecimal();
            _logger.LogDebug("Fetched rate from API: {From}/{To} = {Rate}", fromCurrency, toCurrency, rate);
            return new ExchangeRate(fromCurrency, toCurrency, rate, DateTimeOffset.UtcNow);
        }

        throw new InvalidOperationException("Invalid API response format");
    }

    private static decimal GetFallbackRate(string fromCurrency, string toCurrency)
    {
        // Approximate rates to VND (Jan 2026)
        var toVndRates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = 24500m,
            ["EUR"] = 26500m,
            ["GBP"] = 31000m,
            ["JPY"] = 165m,
            ["CNY"] = 3400m,
            ["KRW"] = 18m,
            ["SGD"] = 18000m,
            ["THB"] = 710m,
            ["AUD"] = 16000m,
            ["CAD"] = 17500m,
            ["CHF"] = 27500m,
            ["VND"] = 1m
        };

        if (!toVndRates.TryGetValue(fromCurrency, out var fromRate) ||
            !toVndRates.TryGetValue(toCurrency, out var toRate))
        {
            throw new InvalidOperationException($"No fallback rate available for {fromCurrency}/{toCurrency}");
        }

        // Convert: fromCurrency -> VND -> toCurrency
        return fromRate / toRate;
    }
}

/// <summary>
/// Settings for currency conversion service.
/// </summary>
public class CurrencySettings
{
    public const string SectionName = "Currency";

    /// <summary>
    /// API key for external exchange rate service.
    /// </summary>
    public string? ExchangeRateApiKey { get; set; }

    /// <summary>
    /// Base URL for exchange rate API.
    /// </summary>
    public string ExchangeRateApiUrl { get; set; } = "https://v6.exchangerate-api.com/v6";

    /// <summary>
    /// How long to cache exchange rates (in minutes).
    /// </summary>
    public int RateCacheMinutes { get; set; } = 60;

    /// <summary>
    /// Manual rate overrides (e.g., "USD_VND" -> 24500).
    /// </summary>
    public Dictionary<string, decimal> ManualRates { get; set; } = new();
}
