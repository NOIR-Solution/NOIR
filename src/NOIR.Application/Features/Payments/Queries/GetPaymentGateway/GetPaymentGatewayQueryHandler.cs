namespace NOIR.Application.Features.Payments.Queries.GetPaymentGateway;

/// <summary>
/// Handler for getting a payment gateway by ID.
/// </summary>
public class GetPaymentGatewayQueryHandler
{
    private readonly IRepository<PaymentGateway, Guid> _gatewayRepository;

    public GetPaymentGatewayQueryHandler(IRepository<PaymentGateway, Guid> gatewayRepository)
    {
        _gatewayRepository = gatewayRepository;
    }

    public async Task<Result<PaymentGatewayDto>> Handle(
        GetPaymentGatewayQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new PaymentGatewayByIdSpec(query.Id);
        var gateway = await _gatewayRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (gateway == null)
        {
            return Result.Failure<PaymentGatewayDto>(
                Error.NotFound("Payment gateway not found.", ErrorCodes.Payment.GatewayNotFound));
        }

        return Result.Success(new PaymentGatewayDto(
            gateway.Id,
            gateway.Provider,
            gateway.DisplayName,
            gateway.IsActive,
            gateway.SortOrder,
            gateway.Environment,
            !string.IsNullOrEmpty(gateway.EncryptedCredentials),
            gateway.WebhookUrl,
            gateway.MinAmount,
            gateway.MaxAmount,
            gateway.SupportedCurrencies,
            gateway.LastHealthCheck,
            gateway.HealthStatus,
            gateway.CreatedAt,
            gateway.ModifiedAt));
    }
}
