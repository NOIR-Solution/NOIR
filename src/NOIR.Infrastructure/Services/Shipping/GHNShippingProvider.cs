namespace NOIR.Infrastructure.Services.Shipping;

/// <summary>
/// GHN (Giao Hàng Nhanh) shipping provider implementation.
/// API Documentation: https://api.ghn.vn/home/docs
/// </summary>
public class GHNShippingProvider : IShippingProvider, IScopedService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICredentialEncryptionService _encryptionService;
    private readonly ILogger<GHNShippingProvider> _logger;

    private const string SANDBOX_URL = "https://dev-online-gateway.ghn.vn";
    private const string PRODUCTION_URL = "https://online-gateway.ghn.vn";

    public ShippingProviderCode ProviderCode => ShippingProviderCode.GHN;
    public string ProviderName => "Giao Hàng Nhanh";

    public GHNShippingProvider(
        IHttpClientFactory httpClientFactory,
        ICredentialEncryptionService encryptionService,
        ILogger<GHNShippingProvider> logger)
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

            // First get available services
            var servicesResult = await GetAvailableServicesAsync(
                request.Origin.DistrictCode,
                request.Destination.DistrictCode,
                client, ct);

            if (servicesResult.IsFailure)
            {
                return Result.Failure<List<ShippingRateDto>>(servicesResult.Error);
            }

            var rates = new List<ShippingRateDto>();

            foreach (var service in servicesResult.Value)
            {
                var feeRequest = new
                {
                    service_id = service.ServiceId,
                    insurance_value = request.RequireInsurance ? (int)request.DeclaredValue : 0,
                    coupon = (string?)null,
                    from_district_id = int.Parse(request.Origin.DistrictCode),
                    to_district_id = int.Parse(request.Destination.DistrictCode),
                    to_ward_code = request.Destination.WardCode,
                    height = request.HeightCm.HasValue ? (int)request.HeightCm.Value : 10,
                    length = request.LengthCm.HasValue ? (int)request.LengthCm.Value : 10,
                    width = request.WidthCm.HasValue ? (int)request.WidthCm.Value : 10,
                    weight = (int)request.WeightGrams,
                    cod_value = request.CodAmount.HasValue ? (int)request.CodAmount.Value : 0
                };

                var response = await client.PostAsJsonAsync("/shiip/public-api/v2/shipping-order/fee", feeRequest, ct);
                var result = await response.Content.ReadFromJsonAsync<GHNFeeResponse>(ct);

                if (result?.Code == 200 && result.Data != null)
                {
                    var codFee = request.CodAmount.HasValue
                        ? CalculateCodFee(request.CodAmount.Value)
                        : 0;

                    rates.Add(new ShippingRateDto(
                        ProviderCode,
                        ProviderName,
                        service.ServiceTypeId.ToString(),
                        service.ShortName,
                        result.Data.Total,
                        codFee,
                        result.Data.Insurance,
                        result.Data.Total + codFee,
                        GetEstimatedDaysMin(service.ServiceTypeId),
                        GetEstimatedDaysMax(service.ServiceTypeId),
                        "VND",
                        service.ShortName));
                }
            }

            return rates.Count > 0
                ? Result.Success(rates)
                : Result.Failure<List<ShippingRateDto>>(
                    Error.Failure(ErrorCodes.External.ServiceUnavailable, "No shipping rates available for this route"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating GHN rates");
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
                payment_type_id = request.IsFreeship ? 1 : 2, // 1 = shop pays, 2 = buyer pays
                note = request.Notes ?? "",
                required_note = "KHONGCHOXEMHANG", // Do not allow inspection
                client_order_code = request.OrderId.ToString(),
                to_name = request.Recipient.FullName,
                to_phone = request.Recipient.Phone,
                to_address = request.DeliveryAddress.AddressLine1,
                to_ward_code = request.DeliveryAddress.WardCode,
                to_district_id = int.Parse(request.DeliveryAddress.DistrictCode),
                cod_amount = request.CodAmount.HasValue ? (int)request.CodAmount.Value : 0,
                content = string.Join(", ", request.Items.Select(i => i.Name)),
                weight = (int)request.TotalWeightGrams,
                length = 10, // Default dimensions
                width = 10,
                height = 10,
                insurance_value = (int)request.DeclaredValue,
                service_type_id = int.Parse(request.ServiceTypeCode),
                items = request.Items.Select(i => new
                {
                    name = i.Name,
                    code = i.Sku ?? "",
                    quantity = i.Quantity,
                    price = (int)i.Value,
                    weight = (int)i.WeightGrams
                }).ToArray()
            };

            var response = await client.PostAsJsonAsync("/shiip/public-api/v2/shipping-order/create", orderRequest, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<GHNOrderResponse>(responseContent);

            if (result?.Code != 200 || result.Data == null)
            {
                return Result.Failure<ProviderShippingOrderResult>(
                    Error.Failure(ErrorCodes.External.ServiceUnavailable, result?.Message ?? "Failed to create order"));
            }

            var trackingNumber = result.Data.OrderCode;
            var trackingUrl = providerConfig.GetTrackingUrl(trackingNumber);

            return Result.Success(new ProviderShippingOrderResult(
                trackingNumber,
                result.Data.OrderCode,
                null, // Label URL would need separate API call
                result.Data.TotalFee,
                null, // COD fee included in total
                null, // Insurance fee included in total
                !string.IsNullOrEmpty(result.Data.ExpectedDeliveryTime)
                    ? DateTimeOffset.Parse(result.Data.ExpectedDeliveryTime)
                    : null,
                responseContent));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating GHN order");
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

            var request = new { order_code = trackingNumber };
            var response = await client.PostAsJsonAsync("/shiip/public-api/v2/shipping-order/detail", request, ct);
            var result = await response.Content.ReadFromJsonAsync<GHNOrderDetailResponse>(ct);

            if (result?.Code != 200 || result.Data == null)
            {
                return Result.Failure<ProviderOrderDetails>(
                    Error.Failure(ErrorCodes.External.ServiceUnavailable, result?.Message ?? "Failed to get order"));
            }

            var status = MapGHNStatus(result.Data.Status);

            return Result.Success(new ProviderOrderDetails(
                trackingNumber,
                result.Data.OrderCode,
                status,
                result.Data.Status,
                result.Data.CurrentWarehouseName,
                !string.IsNullOrEmpty(result.Data.Leadtime)
                    ? DateTimeOffset.Parse(result.Data.Leadtime)
                    : null,
                status == ShippingStatus.Delivered && !string.IsNullOrEmpty(result.Data.FinishDate)
                    ? DateTimeOffset.Parse(result.Data.FinishDate)
                    : null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting GHN order {TrackingNumber}", trackingNumber);
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

            var request = new { order_codes = new[] { trackingNumber } };
            var response = await client.PostAsJsonAsync("/shiip/public-api/v2/switch-status/cancel", request, ct);
            var result = await response.Content.ReadFromJsonAsync<GHNBaseResponse>(ct);

            if (result?.Code != 200)
            {
                return Result.Failure(
                    Error.Failure(ErrorCodes.External.ServiceUnavailable, result?.Message ?? "Failed to cancel order"));
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling GHN order {TrackingNumber}", trackingNumber);
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
            new List<ProviderTrackingEvent>()
        ));
    }

    public Result<ShippingWebhookPayload> ParseWebhook(
        string rawPayload,
        string? signature,
        ShippingProvider providerConfig)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<GHNWebhookPayload>(rawPayload);
            if (payload == null)
            {
                return Result.Failure<ShippingWebhookPayload>(
                    Error.Validation("payload", "Invalid webhook payload"));
            }

            var status = MapGHNStatus(payload.Status);

            return Result.Success(new ShippingWebhookPayload(
                ProviderCode.ToString(),
                payload.OrderCode ?? "",
                payload.Status,
                status.ToString(),
                payload.Description ?? status.ToString(),
                payload.WarehouseName,
                !string.IsNullOrEmpty(payload.Time)
                    ? DateTimeOffset.Parse(payload.Time)
                    : DateTimeOffset.UtcNow,
                rawPayload));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing GHN webhook");
            return Result.Failure<ShippingWebhookPayload>(
                Error.Validation("payload", $"Error parsing webhook: {ex.Message}"));
        }
    }

    public async Task<Result<List<ProviderServiceType>>> GetServiceTypesAsync(
        string originProvinceCode,
        string destinationProvinceCode,
        ShippingProvider providerConfig,
        CancellationToken ct = default)
    {
        try
        {
            var credentials = GetCredentials(providerConfig);
            var client = CreateHttpClient(providerConfig, credentials);

            var servicesResult = await GetAvailableServicesAsync(originProvinceCode, destinationProvinceCode, client, ct);
            if (servicesResult.IsFailure)
            {
                return Result.Failure<List<ProviderServiceType>>(servicesResult.Error);
            }

            var serviceTypes = servicesResult.Value.Select(s => new ProviderServiceType(
                s.ServiceTypeId.ToString(),
                s.ShortName,
                s.ShortName,
                GetEstimatedDaysMin(s.ServiceTypeId),
                GetEstimatedDaysMax(s.ServiceTypeId)
            )).ToList();

            return Result.Success(serviceTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting GHN service types");
            return Result.Failure<List<ProviderServiceType>>(
                Error.Failure(ErrorCodes.External.ServiceUnavailable, $"Error getting service types: {ex.Message}"));
        }
    }

    public async Task<Result<ShippingProviderHealthStatus>> HealthCheckAsync(
        ShippingProvider providerConfig,
        CancellationToken ct = default)
    {
        try
        {
            var credentials = GetCredentials(providerConfig);
            var client = CreateHttpClient(providerConfig, credentials);

            var response = await client.GetAsync("/shiip/public-api/master-data/province", ct);

            return response.IsSuccessStatusCode
                ? Result.Success(ShippingProviderHealthStatus.Healthy)
                : Result.Success(ShippingProviderHealthStatus.Degraded);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GHN health check failed");
            return Result.Success(ShippingProviderHealthStatus.Unhealthy);
        }
    }

    private async Task<Result<List<GHNServiceData>>> GetAvailableServicesAsync(
        string fromDistrictCode,
        string toDistrictCode,
        HttpClient client,
        CancellationToken ct)
    {
        var request = new
        {
            shop_id = int.Parse(client.DefaultRequestHeaders.GetValues("ShopId").FirstOrDefault() ?? "0"),
            from_district = int.Parse(fromDistrictCode),
            to_district = int.Parse(toDistrictCode)
        };

        var response = await client.PostAsJsonAsync("/shiip/public-api/v2/shipping-order/available-services", request, ct);
        var result = await response.Content.ReadFromJsonAsync<GHNServicesResponse>(ct);

        if (result?.Code != 200 || result.Data == null)
        {
            return Result.Failure<List<GHNServiceData>>(
                Error.Failure(ErrorCodes.External.ServiceUnavailable, result?.Message ?? "Failed to get available services"));
        }

        return Result.Success(result.Data);
    }

    private GHNCredentials GetCredentials(ShippingProvider providerConfig)
    {
        if (string.IsNullOrEmpty(providerConfig.EncryptedCredentials))
        {
            throw new InvalidOperationException("GHN credentials not configured");
        }

        var decrypted = _encryptionService.Decrypt(providerConfig.EncryptedCredentials);
        var credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(decrypted);

        return new GHNCredentials(
            credentials?.GetValueOrDefault("Token") ?? throw new InvalidOperationException("GHN Token not found"),
            credentials?.GetValueOrDefault("ShopId") ?? throw new InvalidOperationException("GHN ShopId not found"));
    }

    private HttpClient CreateHttpClient(ShippingProvider providerConfig, GHNCredentials credentials)
    {
        var client = _httpClientFactory.CreateClient("GHN");
        var baseUrl = providerConfig.Environment == GatewayEnvironment.Production
            ? (!string.IsNullOrEmpty(providerConfig.ApiBaseUrl) ? providerConfig.ApiBaseUrl : PRODUCTION_URL)
            : SANDBOX_URL;

        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("Token", credentials.Token);
        client.DefaultRequestHeaders.Add("ShopId", credentials.ShopId);

        return client;
    }

    private static decimal CalculateCodFee(decimal codAmount)
    {
        // GHN COD fee is typically 1% with minimum 16,500 VND
        var fee = codAmount * 0.01m;
        return Math.Max(fee, 16500);
    }

    private static int GetEstimatedDaysMin(int serviceTypeId) => serviceTypeId switch
    {
        2 => 1, // Express
        1 => 2, // Standard
        _ => 3  // Economy
    };

    private static int GetEstimatedDaysMax(int serviceTypeId) => serviceTypeId switch
    {
        2 => 2, // Express
        1 => 3, // Standard
        _ => 5  // Economy
    };

    private static ShippingStatus MapGHNStatus(string status) => status.ToLowerInvariant() switch
    {
        "ready_to_pick" => ShippingStatus.AwaitingPickup,
        "picking" => ShippingStatus.AwaitingPickup,
        "cancel" => ShippingStatus.Cancelled,
        "money_collect_picking" => ShippingStatus.PickedUp,
        "picked" => ShippingStatus.PickedUp,
        "storing" => ShippingStatus.InTransit,
        "transporting" => ShippingStatus.InTransit,
        "sorting" => ShippingStatus.InTransit,
        "delivering" => ShippingStatus.OutForDelivery,
        "delivered" => ShippingStatus.Delivered,
        "delivery_fail" => ShippingStatus.DeliveryFailed,
        "waiting_to_return" => ShippingStatus.Returning,
        "return" => ShippingStatus.Returning,
        "return_transporting" => ShippingStatus.Returning,
        "return_sorting" => ShippingStatus.Returning,
        "returning" => ShippingStatus.Returning,
        "returned" => ShippingStatus.Returned,
        _ => ShippingStatus.InTransit
    };

    private record GHNCredentials(string Token, string ShopId);
}

// GHN API Response Models
internal record GHNBaseResponse
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

internal record GHNFeeResponse : GHNBaseResponse
{
    [JsonPropertyName("data")]
    public GHNFeeData? Data { get; init; }
}

internal record GHNFeeData
{
    [JsonPropertyName("total")]
    public decimal Total { get; init; }

    [JsonPropertyName("service_fee")]
    public decimal ServiceFee { get; init; }

    [JsonPropertyName("insurance")]
    public decimal Insurance { get; init; }

    [JsonPropertyName("cod_fee")]
    public decimal CodFee { get; init; }
}

internal record GHNServicesResponse : GHNBaseResponse
{
    [JsonPropertyName("data")]
    public List<GHNServiceData>? Data { get; init; }
}

internal record GHNServiceData
{
    [JsonPropertyName("service_id")]
    public int ServiceId { get; init; }

    [JsonPropertyName("short_name")]
    public string ShortName { get; init; } = "";

    [JsonPropertyName("service_type_id")]
    public int ServiceTypeId { get; init; }
}

internal record GHNOrderResponse : GHNBaseResponse
{
    [JsonPropertyName("data")]
    public GHNOrderData? Data { get; init; }
}

internal record GHNOrderData
{
    [JsonPropertyName("order_code")]
    public string OrderCode { get; init; } = "";

    [JsonPropertyName("total_fee")]
    public decimal TotalFee { get; init; }

    [JsonPropertyName("expected_delivery_time")]
    public string? ExpectedDeliveryTime { get; init; }
}

internal record GHNOrderDetailResponse : GHNBaseResponse
{
    [JsonPropertyName("data")]
    public GHNOrderDetailData? Data { get; init; }
}

internal record GHNOrderDetailData
{
    [JsonPropertyName("order_code")]
    public string OrderCode { get; init; } = "";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "";

    [JsonPropertyName("current_warehouse_name")]
    public string? CurrentWarehouseName { get; init; }

    [JsonPropertyName("leadtime")]
    public string? Leadtime { get; init; }

    [JsonPropertyName("finish_date")]
    public string? FinishDate { get; init; }
}

internal record GHNWebhookPayload
{
    [JsonPropertyName("OrderCode")]
    public string? OrderCode { get; init; }

    [JsonPropertyName("Status")]
    public string Status { get; init; } = "";

    [JsonPropertyName("Description")]
    public string? Description { get; init; }

    [JsonPropertyName("WarehouseName")]
    public string? WarehouseName { get; init; }

    [JsonPropertyName("Time")]
    public string? Time { get; init; }
}
