namespace NOIR.Application.Features.Shipping.DTOs;

/// <summary>
/// Request for calculating shipping rates across providers.
/// </summary>
public record CalculateShippingRatesRequest(
    ShippingAddressDto Origin,
    ShippingAddressDto Destination,
    decimal WeightGrams,
    decimal? LengthCm = null,
    decimal? WidthCm = null,
    decimal? HeightCm = null,
    decimal DeclaredValue = 0,
    decimal? CodAmount = null,
    bool RequireInsurance = false,
    List<ShippingProviderCode>? PreferredProviders = null);

/// <summary>
/// A single shipping rate option from a provider.
/// </summary>
public record ShippingRateDto(
    ShippingProviderCode ProviderCode,
    string ProviderName,
    string ServiceTypeCode,
    string ServiceTypeName,
    decimal BaseRate,
    decimal CodFee,
    decimal InsuranceFee,
    decimal TotalRate,
    int EstimatedDaysMin,
    int EstimatedDaysMax,
    string Currency = "VND",
    string? Notes = null);

/// <summary>
/// Response containing all available shipping rates.
/// </summary>
public record ShippingRatesResponse(
    List<ShippingRateDto> Rates,
    ShippingRateDto? RecommendedRate,
    string? Message = null);
