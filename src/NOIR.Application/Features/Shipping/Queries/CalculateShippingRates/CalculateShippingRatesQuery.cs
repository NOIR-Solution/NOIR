namespace NOIR.Application.Features.Shipping.Queries.CalculateShippingRates;

/// <summary>
/// Query to calculate shipping rates from all active providers.
/// </summary>
public sealed record CalculateShippingRatesQuery(
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
