namespace NOIR.Application.Features.Shipping.Queries.GetActiveShippingProviders;

/// <summary>
/// Handler for getting active shipping providers for checkout.
/// </summary>
public class GetActiveShippingProvidersQueryHandler
{
    private readonly IRepository<ShippingProvider, Guid> _providerRepository;

    public GetActiveShippingProvidersQueryHandler(IRepository<ShippingProvider, Guid> providerRepository)
    {
        _providerRepository = providerRepository;
    }

    public async Task<Result<List<CheckoutShippingProviderDto>>> Handle(
        GetActiveShippingProvidersQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new ActiveShippingProvidersSpec();
        var providers = await _providerRepository.ListAsync(spec, cancellationToken);

        var items = providers.Select(p => p.ToCheckoutDto()).ToList();

        return Result.Success(items);
    }
}
