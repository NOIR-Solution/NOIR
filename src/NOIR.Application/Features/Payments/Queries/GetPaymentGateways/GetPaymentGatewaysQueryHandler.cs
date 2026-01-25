namespace NOIR.Application.Features.Payments.Queries.GetPaymentGateways;

/// <summary>
/// Handler for getting all payment gateways.
/// </summary>
public class GetPaymentGatewaysQueryHandler
{
    private readonly IRepository<PaymentGateway, Guid> _gatewayRepository;

    public GetPaymentGatewaysQueryHandler(IRepository<PaymentGateway, Guid> gatewayRepository)
    {
        _gatewayRepository = gatewayRepository;
    }

    public async Task<Result<List<PaymentGatewayDto>>> Handle(
        GetPaymentGatewaysQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new PaymentGatewaysSpec();
        var gateways = await _gatewayRepository.ListAsync(spec, cancellationToken);

        var items = gateways.Select(g => new PaymentGatewayDto(
            g.Id,
            g.Provider,
            g.DisplayName,
            g.IsActive,
            g.SortOrder,
            g.Environment,
            !string.IsNullOrEmpty(g.EncryptedCredentials),
            g.WebhookUrl,
            g.MinAmount,
            g.MaxAmount,
            g.SupportedCurrencies,
            g.LastHealthCheck,
            g.HealthStatus,
            g.CreatedAt,
            g.ModifiedAt)).ToList();

        return Result.Success(items);
    }
}
