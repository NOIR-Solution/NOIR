namespace NOIR.Application.Features.Payments.Commands.ConfirmCodCollection;

/// <summary>
/// Handler for confirming COD payment collection.
/// </summary>
public class ConfirmCodCollectionCommandHandler
{
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmCodCollectionCommandHandler(
        IRepository<PaymentTransaction, Guid> paymentRepository,
        IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PaymentTransactionDto>> Handle(
        ConfirmCodCollectionCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new PaymentTransactionByIdForUpdateSpec(command.PaymentTransactionId);
        var payment = await _paymentRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (payment == null)
        {
            return Result.Failure<PaymentTransactionDto>(
                Error.NotFound("Payment transaction not found.", ErrorCodes.Payment.TransactionNotFound));
        }

        // Validate it's a COD payment
        if (payment.PaymentMethod != PaymentMethod.COD)
        {
            return Result.Failure<PaymentTransactionDto>(
                Error.Validation("PaymentMethod", "This is not a COD payment.", ErrorCodes.Payment.NotCodPayment));
        }

        // Validate status
        if (payment.Status != PaymentStatus.CodPending)
        {
            return Result.Failure<PaymentTransactionDto>(
                Error.Validation("Status", "Payment is not in COD pending status.", ErrorCodes.Payment.InvalidStatusTransition));
        }

        // Validate collector name
        if (string.IsNullOrEmpty(command.UserId))
        {
            return Result.Failure<PaymentTransactionDto>(
                Error.Validation("UserId", "Invalid collector ID.", ErrorCodes.Payment.InvalidRequesterId));
        }

        payment.ConfirmCodCollection(command.UserId);
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
