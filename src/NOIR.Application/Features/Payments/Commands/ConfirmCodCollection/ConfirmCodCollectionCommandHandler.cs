namespace NOIR.Application.Features.Payments.Commands.ConfirmCodCollection;

/// <summary>
/// Handler for confirming COD payment collection.
/// </summary>
public class ConfirmCodCollectionCommandHandler
{
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentHubContext _paymentHubContext;
    private readonly ICurrentUser _currentUser;

    public ConfirmCodCollectionCommandHandler(
        IRepository<PaymentTransaction, Guid> paymentRepository,
        IRepository<Order, Guid> orderRepository,
        IUnitOfWork unitOfWork,
        IPaymentHubContext paymentHubContext,
        ICurrentUser currentUser)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _paymentHubContext = paymentHubContext;
        _currentUser = currentUser;
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

        // Use display name or email instead of raw UserId (GUID) for collector name
        // collectorName cannot be null: command.UserId guard above ensures at least UserId is non-null
        var collectorName = _currentUser.DisplayName ?? _currentUser.Email ?? command.UserId!;
        payment.ConfirmCodCollection(collectorName);

        // Persist collection notes in metadata if provided
        if (!string.IsNullOrWhiteSpace(command.Notes))
        {
            payment.SetMetadataJson(System.Text.Json.JsonSerializer.Serialize(new { CollectionNotes = command.Notes }));
        }

        // Confirm the order if it's still Pending
        if (payment.OrderId.HasValue)
        {
            var orderSpec = new OrderByIdForUpdateSpec(payment.OrderId.Value);
            var order = await _orderRepository.FirstOrDefaultAsync(orderSpec, cancellationToken);

            if (order != null && order.Status == OrderStatus.Pending)
            {
                order.Confirm();
            }
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<PaymentTransactionDto>(
                Error.Conflict("This payment was modified by another user. Please refresh and try again.", ErrorCodes.Payment.ConcurrencyConflict));
        }

        // Send real-time notification for COD collection
        if (!string.IsNullOrEmpty(_currentUser.TenantId))
        {
            await _paymentHubContext.SendCodCollectionUpdateAsync(
                _currentUser.TenantId,
                payment.Id,
                payment.TransactionNumber,
                payment.CodCollectorName ?? command.UserId,
                payment.Amount,
                payment.Currency,
                cancellationToken);
        }

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
