namespace NOIR.Application.Features.Payments.Commands.CancelPayment;

/// <summary>
/// Handler for cancelling a pending payment.
/// </summary>
public class CancelPaymentCommandHandler
{
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelPaymentCommandHandler(
        IRepository<PaymentTransaction, Guid> paymentRepository,
        IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PaymentTransactionDto>> Handle(
        CancelPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new PaymentTransactionByIdForUpdateSpec(command.PaymentTransactionId);
        var payment = await _paymentRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (payment == null)
        {
            return Result.Failure<PaymentTransactionDto>(
                Error.NotFound("Payment transaction not found.", ErrorCodes.Payment.TransactionNotFound));
        }

        // Only pending or requires-action payments can be cancelled
        if (payment.Status != PaymentStatus.Pending && payment.Status != PaymentStatus.RequiresAction)
        {
            return Result.Failure<PaymentTransactionDto>(
                Error.Validation("Status", "Only pending payments can be cancelled.", ErrorCodes.Payment.InvalidStatusTransition));
        }

        payment.MarkAsCancelled();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(payment));
    }

    private static PaymentTransactionDto MapToDto(PaymentTransaction payment)
    {
        return new PaymentTransactionDto(
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
            payment.ModifiedAt);
    }
}
