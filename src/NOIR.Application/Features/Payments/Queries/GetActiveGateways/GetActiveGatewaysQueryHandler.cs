namespace NOIR.Application.Features.Payments.Queries.GetActiveGateways;

/// <summary>
/// Handler for getting active payment gateways for checkout.
/// </summary>
public class GetActiveGatewaysQueryHandler
{
    private readonly IRepository<PaymentGateway, Guid> _gatewayRepository;

    public GetActiveGatewaysQueryHandler(IRepository<PaymentGateway, Guid> gatewayRepository)
    {
        _gatewayRepository = gatewayRepository;
    }

    public async Task<Result<List<CheckoutGatewayDto>>> Handle(
        GetActiveGatewaysQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new ActivePaymentGatewaysSpec();
        var gateways = await _gatewayRepository.ListAsync(spec, cancellationToken);

        var items = gateways.Select(g => new CheckoutGatewayDto(
            g.Id,
            g.Provider,
            g.DisplayName,
            g.SortOrder,
            g.MinAmount,
            g.MaxAmount,
            g.SupportedCurrencies)).ToList();

        return Result.Success(items);
    }
}
