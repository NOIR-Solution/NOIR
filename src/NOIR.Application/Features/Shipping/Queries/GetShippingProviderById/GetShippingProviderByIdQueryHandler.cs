namespace NOIR.Application.Features.Shipping.Queries.GetShippingProviderById;

/// <summary>
/// Handler for getting a shipping provider by ID.
/// </summary>
public class GetShippingProviderByIdQueryHandler
{
    private readonly IRepository<ShippingProvider, Guid> _providerRepository;

    public GetShippingProviderByIdQueryHandler(IRepository<ShippingProvider, Guid> providerRepository)
    {
        _providerRepository = providerRepository;
    }

    public async Task<Result<ShippingProviderDto>> Handle(
        GetShippingProviderByIdQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new ShippingProviderByIdSpec(query.ProviderId);
        var provider = await _providerRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (provider == null)
        {
            return Result.Failure<ShippingProviderDto>(
                Error.NotFound("Shipping provider not found.", ErrorCodes.Shipping.ProviderNotFound));
        }

        return Result.Success(provider.ToDto());
    }
}
