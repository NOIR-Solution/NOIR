namespace NOIR.Application.Features.Payments.Queries.GetPaymentTransaction;

/// <summary>
/// Handler for getting a payment transaction by ID.
/// </summary>
public class GetPaymentTransactionQueryHandler
{
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;

    public GetPaymentTransactionQueryHandler(IRepository<PaymentTransaction, Guid> paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<PaymentTransactionDto>> Handle(
        GetPaymentTransactionQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new PaymentTransactionByIdSpec(query.Id);
        var payment = await _paymentRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (payment == null)
        {
            return Result.Failure<PaymentTransactionDto>(
                Error.NotFound("Payment transaction not found.", ErrorCodes.Payment.TransactionNotFound));
        }

        return Result.Success(new PaymentTransactionDto(
            payment.Id,
            payment.TransactionNumber,
            payment.GatewayTransactionId,
            payment.PaymentGatewayId,
            payment.Provider,
            payment.OrderId,
            payment.CustomerId,
            payment.Amount,
            payment.Currency,
            payment.GatewayFee,
            payment.NetAmount,
            payment.Status,
            payment.FailureReason,
            payment.PaymentMethod,
            payment.PaymentMethodDetail,
            payment.PaidAt,
            payment.ExpiresAt,
            payment.CodCollectorName,
            payment.CodCollectedAt,
            payment.CreatedAt,
            payment.ModifiedAt));
    }
}
