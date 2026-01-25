namespace NOIR.Application.Features.Payments.Queries.GetOrderPayments;

/// <summary>
/// Handler for getting payment transactions for an order.
/// </summary>
public class GetOrderPaymentsQueryHandler
{
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;

    public GetOrderPaymentsQueryHandler(IRepository<PaymentTransaction, Guid> paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<List<PaymentTransactionDto>>> Handle(
        GetOrderPaymentsQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new PaymentTransactionsByOrderSpec(query.OrderId);
        var payments = await _paymentRepository.ListAsync(spec, cancellationToken);

        var items = payments.Select(p => new PaymentTransactionDto(
            p.Id,
            p.TransactionNumber,
            p.GatewayTransactionId,
            p.PaymentGatewayId,
            p.Provider,
            p.OrderId,
            p.CustomerId,
            p.Amount,
            p.Currency,
            p.GatewayFee,
            p.NetAmount,
            p.Status,
            p.FailureReason,
            p.PaymentMethod,
            p.PaymentMethodDetail,
            p.PaidAt,
            p.ExpiresAt,
            p.CodCollectorName,
            p.CodCollectedAt,
            p.CreatedAt,
            p.ModifiedAt)).ToList();

        return Result.Success(items);
    }
}
