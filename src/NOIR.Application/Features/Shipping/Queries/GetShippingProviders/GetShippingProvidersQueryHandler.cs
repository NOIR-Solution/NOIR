namespace NOIR.Application.Features.Shipping.Queries.GetShippingProviders;

/// <summary>
/// Handler for getting all shipping providers.
/// </summary>
public class GetShippingProvidersQueryHandler
{
    private readonly IRepository<ShippingProvider, Guid> _providerRepository;

    public GetShippingProvidersQueryHandler(IRepository<ShippingProvider, Guid> providerRepository)
    {
        _providerRepository = providerRepository;
    }

    public async Task<Result<List<ShippingProviderDto>>> Handle(
        GetShippingProvidersQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new ShippingProvidersSpec();
        var providers = await _providerRepository.ListAsync(spec, cancellationToken);

        var items = providers.Select(p => p.ToDto()).ToList();

        return Result.Success(items);
    }
}
