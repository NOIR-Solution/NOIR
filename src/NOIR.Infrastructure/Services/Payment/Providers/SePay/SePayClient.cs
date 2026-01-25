using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NOIR.Infrastructure.Services.Payment.Providers.SePay;

/// <summary>
/// HTTP client for SePay API operations.
/// </summary>
public class SePayClient : ISePayClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SePayClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SePayClient(HttpClient httpClient, ILogger<SePayClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SePayTransactionListResponse?> GetTransactionsAsync(
        string apiToken,
        string? accountNumber = null,
        string? transactionId = null,
        CancellationToken ct = default)
    {
        try
        {
            var url = "transactions/list";
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(accountNumber))
            {
                queryParams.Add($"account_number={Uri.EscapeDataString(accountNumber)}");
            }

            if (!string.IsNullOrEmpty(transactionId))
            {
                queryParams.Add($"transaction_id={Uri.EscapeDataString(transactionId)}");
            }

            if (queryParams.Count > 0)
            {
                url += "?" + string.Join("&", queryParams);
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {apiToken}");

            var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "SePay API returned {StatusCode}: {Error}",
                    response.StatusCode,
                    errorContent);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<SePayTransactionListResponse>(JsonOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SePay transactions");
            return null;
        }
    }

    public async Task<SePayAccountResponse?> GetAccountInfoAsync(
        string apiToken,
        CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "bankaccounts");
            request.Headers.Add("Authorization", $"Bearer {apiToken}");

            var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "SePay account info API returned {StatusCode}: {Error}",
                    response.StatusCode,
                    errorContent);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<SePayAccountResponse>(JsonOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SePay account info");
            return null;
        }
    }
}
