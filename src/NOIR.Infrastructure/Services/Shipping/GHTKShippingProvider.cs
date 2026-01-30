namespace NOIR.Infrastructure.Services.Shipping;

/// <summary>
/// GHTK (Giao Hàng Tiết Kiệm) shipping provider implementation.
/// API Documentation: https://pro-docs.ghtk.vn/
/// </summary>
public class GHTKShippingProvider : IShippingProvider, IScopedService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICredentialEncryptionService _encryptionService;
    private readonly ILogger<GHTKShippingProvider> _logger;

    private const string SANDBOX_URL = "https://services-staging.ghtklab.com";
    private const string PRODUCTION_URL = "https://services.giaohangtietkiem.vn";

    public ShippingProviderCode ProviderCode => ShippingProviderCode.GHTK;
    public string ProviderName => "Giao Hàng Tiết Kiệm";

    public GHTKShippingProvider(
        IHttpClientFactory httpClientFactory,
        ICredentialEncryptionService encryptionService,
        ILogger<GHTKShippingProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<Result<List<ShippingRateDto>>> CalculateRatesAsync(
        CalculateShippingRatesRequest request,
        ShippingProvider providerConfig,
        CancellationToken ct = default)
    {
        try
        {
            var credentials = GetCredentials(providerConfig);
            var client = CreateHttpClient(providerConfig, credentials);

            var feeRequest = new
            {
                pick_province = request.Origin.Province,
                pick_district = request.Origin.District,
                pick_ward = request.Origin.Ward,
                province = request.Destination.Province,
                district = request.Destination.District,
                ward = request.Destination.Ward,
                weight = (int)request.WeightGrams,
                value = (int)request.DeclaredValue,
                transport = "road", // Default to road transport
                deliver_option = "none" // Standard delivery
            };

            var response = await client.PostAsJsonAsync("/services/shipment/fee", feeRequest, ct);
            var result = await response.Content.ReadFromJsonAsync<GHTKFeeResponse>(ct);

            if (result?.Success != true)
            {
                return Result.Failure<List<ShippingRateDto>>(
                    Error.Failure(ErrorCodes.External.ServiceUnavailable, result?.Message ?? "Failed to calculate rates"));
            }

            var rates = new List<ShippingRateDto>
            {
                new(
                    ProviderCode,
                    ProviderName,
                    "STANDARD",
                    "Giao hàng tiêu chuẩn",
                    result.Fee?.Fee ?? 0,
                    request.CodAmount.HasValue ? CalculateCodFee(request.CodAmount.Value) : 0,
                    request.RequireInsurance ? CalculateInsuranceFee(request.DeclaredValue) : 0,
                    (result.Fee?.Fee ?? 0) +
                        (request.CodAmount.HasValue ? CalculateCodFee(request.CodAmount.Value) : 0) +
                        (request.RequireInsurance ? CalculateInsuranceFee(request.DeclaredValue) : 0),
                    2, 4, "VND", null)
            };

            // Add express option if available
            if (result.Fee?.Options?.XteamFee > 0)
            {
                rates.Add(new(
                    ProviderCode,
                    ProviderName,
                    "EXPRESS",
                    "Giao hàng nhanh",
                    result.Fee.Options.XteamFee,
                    request.CodAmount.HasValue ? CalculateCodFee(request.CodAmount.Value) : 0,
                    request.RequireInsurance ? CalculateInsuranceFee(request.DeclaredValue) : 0,
                    result.Fee.Options.XteamFee +
                        (request.CodAmount.HasValue ? CalculateCodFee(request.CodAmount.Value) : 0) +
                        (request.RequireInsurance ? CalculateInsuranceFee(request.DeclaredValue) : 0),
                    1, 2, "VND", null));
            }

            return Result.Success(rates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating GHTK rates");
            return Result.Failure<List<ShippingRateDto>>(
                Error.Failure(ErrorCodes.External.ServiceUnavailable, $"Error calculating rates: {ex.Message}"));
        }
    }

    public async Task<Result<ProviderShippingOrderResult>> CreateOrderAsync(
        CreateShippingOrderRequest request,
        ShippingProvider providerConfig,
        CancellationToken ct = default)
    {
        try
        {
            var credentials = GetCredentials(providerConfig);
            var client = CreateHttpClient(providerConfig, credentials);

            var orderRequest = new
            {
                products = request.Items.Select(i => new
                {
                    name = i.Name,
                    weight = i.WeightGrams / 1000m, // Convert to kg
                    quantity = i.Quantity,
                    product_code = i.Sku ?? ""
                }).ToArray(),
                order = new
                {
                    id = request.OrderId.ToString(),
                    pick_name = request.Sender.FullName,
                    pick_tel = request.Sender.Phone,
                    pick_address = request.PickupAddress.AddressLine1,
                    pick_province = request.PickupAddress.Province,
                    pick_district = request.PickupAddress.District,
                    pick_ward = request.PickupAddress.Ward,
                    name = request.Recipient.FullName,
                    tel = request.Recipient.Phone,
                    address = request.DeliveryAddress.AddressLine1,
                    province = request.DeliveryAddress.Province,
                    district = request.DeliveryAddress.District,
                    ward = request.DeliveryAddress.Ward,
                    hamlet = "Khác",
                    is_freeship = request.IsFreeship ? 1 : 0,
                    pick_money = request.CodAmount.HasValue ? (int)request.CodAmount.Value : 0,
                    value = (int)request.DeclaredValue,
                    note = request.Notes ?? "",
                    transport = request.ServiceTypeCode == "EXPRESS" ? "fly" : "road"
                }
            };

            var response = await client.PostAsJsonAsync("/services/shipment/order", orderRequest, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<GHTKOrderResponse>(responseContent);

            if (result?.Success != true)
            {
                return Result.Failure<ProviderShippingOrderResult>(
                    Error.Failure(ErrorCodes.External.ServiceUnavailable, result?.Message ?? "Failed to create order"));
            }

            var trackingNumber = result.Order?.Label ?? throw new InvalidOperationException("No tracking number returned");
            var trackingUrl = providerConfig.GetTrackingUrl(trackingNumber);

            return Result.Success(new ProviderShippingOrderResult(
                trackingNumber,
                result.Order.PartnerId,
                null, // GHTK doesn't return label URL directly
                result.Order.Fee ?? 0,
                result.Order.CodFee,
                result.Order.InsuranceFee,
                result.Order.EstimatedDeliverTime.HasValue
                    ? DateTimeOffset.FromUnixTimeSeconds(result.Order.EstimatedDeliverTime.Value)
                    : null,
                responseContent));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating GHTK order");
            return Result.Failure<ProviderShippingOrderResult>(
                Error.Failure(ErrorCodes.External.ServiceUnavailable, $"Error creating order: {ex.Message}"));
        }
    }

    public async Task<Result<ProviderOrderDetails>> GetOrderAsync(
        string trackingNumber,
        ShippingProvider providerConfig,
        CancellationToken ct = default)
    {
        try
        {
            var credentials = GetCredentials(providerConfig);
            var client = CreateHttpClient(providerConfig, credentials);

            var response = await client.GetAsync($"/services/shipment/v2/{trackingNumber}", ct);
            var result = await response.Content.ReadFromJsonAsync<GHTKOrderStatusResponse>(ct);

            if (result?.Success != true)
            {
                return Result.Failure<ProviderOrderDetails>(
                    Error.Failure(ErrorCodes.External.ServiceUnavailable, result?.Message ?? "Failed to get order"));
            }

            var status = MapGHTKStatus(result.Order?.Status ?? 0);

            return Result.Success(new ProviderOrderDetails(
                trackingNumber,
                result.Order?.PartnerId,
                status,
                result.Order?.StatusText ?? status.ToString(),
                null,
                result.Order?.EstimatedDeliverTime.HasValue == true
                    ? DateTimeOffset.FromUnixTimeSeconds(result.Order.EstimatedDeliverTime.Value)
                    : null,
                null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting GHTK order {TrackingNumber}", trackingNumber);
            return Result.Failure<ProviderOrderDetails>(
                Error.Failure(ErrorCodes.External.ServiceUnavailable, $"Error getting order: {ex.Message}"));
        }
    }

    public async Task<Result> CancelOrderAsync(
        string trackingNumber,
        ShippingProvider providerConfig,
        CancellationToken ct = default)
    {
        try
        {
            var credentials = GetCredentials(providerConfig);
            var client = CreateHttpClient(providerConfig, credentials);

            var response = await client.PostAsync($"/services/shipment/cancel/{trackingNumber}", null, ct);
            var result = await response.Content.ReadFromJsonAsync<GHTKBaseResponse>(ct);

            if (result?.Success != true)
            {
                return Result.Failure(
                    Error.Failure(ErrorCodes.External.ServiceUnavailable, result?.Message ?? "Failed to cancel order"));
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling GHTK order {TrackingNumber}", trackingNumber);
            return Result.Failure(
                Error.Failure(ErrorCodes.External.ServiceUnavailable, $"Error cancelling order: {ex.Message}"));
        }
    }

    public async Task<Result<ProviderTrackingInfo>> GetTrackingAsync(
        string trackingNumber,
        ShippingProvider providerConfig,
        CancellationToken ct = default)
    {
        var orderResult = await GetOrderAsync(trackingNumber, providerConfig, ct);
        if (orderResult.IsFailure)
        {
            return Result.Failure<ProviderTrackingInfo>(orderResult.Error);
        }

        var order = orderResult.Value;
        return Result.Success(new ProviderTrackingInfo(
            trackingNumber,
            order.Status,
            order.StatusDescription,
            order.CurrentLocation,
            order.EstimatedDeliveryDate,
            order.ActualDeliveryDate,
            new List<ProviderTrackingEvent>() // GHTK doesn't return detailed events in status API
        ));
    }

    public Result<ShippingWebhookPayload> ParseWebhook(
        string rawPayload,
        string? signature,
        ShippingProvider providerConfig)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<GHTKWebhookPayload>(rawPayload);
            if (payload == null)
            {
                return Result.Failure<ShippingWebhookPayload>(
                    Error.Validation("payload", "Invalid webhook payload"));
            }

            var status = MapGHTKStatus(payload.Status);

            return Result.Success(new ShippingWebhookPayload(
                ProviderCode.ToString(),
                payload.Label ?? payload.PartnerId ?? "",
                payload.StatusId?.ToString() ?? "STATUS_CHANGE",
                status.ToString(),
                payload.StatusText ?? status.ToString(),
                payload.PickAddress ?? payload.DeliverAddress,
                payload.UpdateTime.HasValue
                    ? DateTimeOffset.FromUnixTimeSeconds(payload.UpdateTime.Value)
                    : DateTimeOffset.UtcNow,
                rawPayload));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing GHTK webhook");
            return Result.Failure<ShippingWebhookPayload>(
                Error.Validation("payload", $"Error parsing webhook: {ex.Message}"));
        }
    }

    public Task<Result<List<ProviderServiceType>>> GetServiceTypesAsync(
        string originProvinceCode,
        string destinationProvinceCode,
        ShippingProvider providerConfig,
        CancellationToken ct = default)
    {
        // GHTK service types are fixed
        var services = new List<ProviderServiceType>
        {
            new("STANDARD", "Giao hàng tiêu chuẩn", "Giao hàng trong 2-4 ngày", 2, 4),
            new("EXPRESS", "Giao hàng nhanh", "Giao hàng trong 1-2 ngày", 1, 2)
        };

        return Task.FromResult(Result.Success(services));
    }

    public async Task<Result<ShippingProviderHealthStatus>> HealthCheckAsync(
        ShippingProvider providerConfig,
        CancellationToken ct = default)
    {
        try
        {
            var credentials = GetCredentials(providerConfig);
            var client = CreateHttpClient(providerConfig, credentials);

            // Use fee calculation as health check (lightweight endpoint)
            var testRequest = new
            {
                pick_province = "Hà Nội",
                pick_district = "Quận Cầu Giấy",
                province = "Hồ Chí Minh",
                district = "Quận 1",
                weight = 500
            };

            var response = await client.PostAsJsonAsync("/services/shipment/fee", testRequest, ct);

            return response.IsSuccessStatusCode
                ? Result.Success(ShippingProviderHealthStatus.Healthy)
                : Result.Success(ShippingProviderHealthStatus.Degraded);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GHTK health check failed");
            return Result.Success(ShippingProviderHealthStatus.Unhealthy);
        }
    }

    private GHTKCredentials GetCredentials(ShippingProvider providerConfig)
    {
        if (string.IsNullOrEmpty(providerConfig.EncryptedCredentials))
        {
            throw new InvalidOperationException("GHTK credentials not configured");
        }

        var decrypted = _encryptionService.Decrypt(providerConfig.EncryptedCredentials);
        var credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(decrypted);

        return new GHTKCredentials(
            credentials?.GetValueOrDefault("ApiToken") ?? throw new InvalidOperationException("GHTK ApiToken not found"));
    }

    private HttpClient CreateHttpClient(ShippingProvider providerConfig, GHTKCredentials credentials)
    {
        var client = _httpClientFactory.CreateClient("GHTK");
        var baseUrl = providerConfig.Environment == GatewayEnvironment.Production
            ? (!string.IsNullOrEmpty(providerConfig.ApiBaseUrl) ? providerConfig.ApiBaseUrl : PRODUCTION_URL)
            : SANDBOX_URL;

        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("Token", credentials.ApiToken);
        client.DefaultRequestHeaders.Add("X-Client-Source", "NOIR");

        return client;
    }

    private static decimal CalculateCodFee(decimal codAmount)
    {
        // GHTK COD fee is typically 1% with minimum 15,000 VND
        var fee = codAmount * 0.01m;
        return Math.Max(fee, 15000);
    }

    private static decimal CalculateInsuranceFee(decimal declaredValue)
    {
        // Insurance is typically 0.5% of declared value
        return declaredValue * 0.005m;
    }

    private static ShippingStatus MapGHTKStatus(int status) => status switch
    {
        -1 => ShippingStatus.Cancelled,
        1 => ShippingStatus.AwaitingPickup,
        2 => ShippingStatus.PickedUp,
        3 => ShippingStatus.InTransit,
        4 => ShippingStatus.InTransit,
        5 => ShippingStatus.Delivered,
        6 => ShippingStatus.Returning,
        7 => ShippingStatus.Returned,
        8 => ShippingStatus.OutForDelivery,
        9 => ShippingStatus.DeliveryFailed,
        _ => ShippingStatus.InTransit
    };

    private record GHTKCredentials(string ApiToken);
}

// GHTK API Response Models
internal record GHTKBaseResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

internal record GHTKFeeResponse : GHTKBaseResponse
{
    [JsonPropertyName("fee")]
    public GHTKFeeData? Fee { get; init; }
}

internal record GHTKFeeData
{
    [JsonPropertyName("fee")]
    public decimal Fee { get; init; }

    [JsonPropertyName("insurance_fee")]
    public decimal InsuranceFee { get; init; }

    [JsonPropertyName("options")]
    public GHTKFeeOptions? Options { get; init; }
}

internal record GHTKFeeOptions
{
    [JsonPropertyName("xteam")]
    public decimal XteamFee { get; init; }
}

internal record GHTKOrderResponse : GHTKBaseResponse
{
    [JsonPropertyName("order")]
    public GHTKOrderData? Order { get; init; }
}

internal record GHTKOrderData
{
    [JsonPropertyName("label")]
    public string? Label { get; init; }

    [JsonPropertyName("partner_id")]
    public string? PartnerId { get; init; }

    [JsonPropertyName("fee")]
    public decimal? Fee { get; init; }

    [JsonPropertyName("cod_fee")]
    public decimal? CodFee { get; init; }

    [JsonPropertyName("insurance_fee")]
    public decimal? InsuranceFee { get; init; }

    [JsonPropertyName("estimated_deliver_time")]
    public long? EstimatedDeliverTime { get; init; }
}

internal record GHTKOrderStatusResponse : GHTKBaseResponse
{
    [JsonPropertyName("order")]
    public GHTKOrderStatusData? Order { get; init; }
}

internal record GHTKOrderStatusData
{
    [JsonPropertyName("label_id")]
    public string? LabelId { get; init; }

    [JsonPropertyName("partner_id")]
    public string? PartnerId { get; init; }

    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("status_text")]
    public string? StatusText { get; init; }

    [JsonPropertyName("estimated_deliver_time")]
    public long? EstimatedDeliverTime { get; init; }
}

internal record GHTKWebhookPayload
{
    [JsonPropertyName("label")]
    public string? Label { get; init; }

    [JsonPropertyName("partner_id")]
    public string? PartnerId { get; init; }

    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("status_id")]
    public int? StatusId { get; init; }

    [JsonPropertyName("status_text")]
    public string? StatusText { get; init; }

    [JsonPropertyName("pick_address")]
    public string? PickAddress { get; init; }

    [JsonPropertyName("deliver_address")]
    public string? DeliverAddress { get; init; }

    [JsonPropertyName("update_time")]
    public long? UpdateTime { get; init; }
}
