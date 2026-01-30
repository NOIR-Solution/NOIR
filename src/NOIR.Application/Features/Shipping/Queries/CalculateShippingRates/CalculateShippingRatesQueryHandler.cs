namespace NOIR.Application.Features.Shipping.Queries.CalculateShippingRates;

/// <summary>
/// Handler for calculating shipping rates from multiple providers.
/// </summary>
public class CalculateShippingRatesQueryHandler
{
    private readonly IRepository<ShippingProvider, Guid> _providerRepository;
    private readonly IShippingProviderFactory _providerFactory;
    private readonly ILogger<CalculateShippingRatesQueryHandler> _logger;

    public CalculateShippingRatesQueryHandler(
        IRepository<ShippingProvider, Guid> providerRepository,
        IShippingProviderFactory providerFactory,
        ILogger<CalculateShippingRatesQueryHandler> logger)
    {
        _providerRepository = providerRepository;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<Result<ShippingRatesResponse>> Handle(
        CalculateShippingRatesQuery query,
        CancellationToken cancellationToken)
    {
        // Get all active providers
        var spec = new ActiveShippingProvidersSpec();
        var providers = await _providerRepository.ListAsync(spec, cancellationToken);

        // Filter by preferred providers if specified
        if (query.PreferredProviders?.Count > 0)
        {
            providers = providers
                .Where(p => query.PreferredProviders.Contains(p.ProviderCode))
                .ToList();
        }

        if (providers.Count == 0)
        {
            return Result.Failure<ShippingRatesResponse>(
                Error.NotFound("No active shipping providers are available for this route.", ErrorCodes.Shipping.ProviderNotFound));
        }

        var request = new CalculateShippingRatesRequest(
            query.Origin,
            query.Destination,
            query.WeightGrams,
            query.LengthCm,
            query.WidthCm,
            query.HeightCm,
            query.DeclaredValue,
            query.CodAmount,
            query.RequireInsurance,
            query.PreferredProviders);

        var allRates = new List<ShippingRateDto>();

        // Calculate rates from each provider in parallel
        var tasks = providers.Select(async providerConfig =>
        {
            var provider = _providerFactory.GetProvider(providerConfig.ProviderCode);
            if (provider == null)
            {
                _logger.LogWarning("No implementation for provider {ProviderCode}", providerConfig.ProviderCode);
                return new List<ShippingRateDto>();
            }

            try
            {
                var result = await provider.CalculateRatesAsync(request, providerConfig, cancellationToken);
                if (result.IsSuccess)
                {
                    return result.Value;
                }

                _logger.LogWarning("Failed to calculate rates from {Provider}: {Error}",
                    providerConfig.ProviderCode, result.Error.Message);
                return new List<ShippingRateDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating rates from {Provider}", providerConfig.ProviderCode);
                return new List<ShippingRateDto>();
            }
        });

        var results = await Task.WhenAll(tasks);
        foreach (var rates in results)
        {
            allRates.AddRange(rates);
        }

        if (allRates.Count == 0)
        {
            return Result.Failure<ShippingRatesResponse>(
                Error.Failure(ErrorCodes.Shipping.RateCalculationFailed, "No shipping rates available from any provider."));
        }

        // Sort by total rate and select recommended
        allRates = allRates.OrderBy(r => r.TotalRate).ToList();
        var recommended = allRates.First();

        return Result.Success(new ShippingRatesResponse(allRates, recommended));
    }
}
