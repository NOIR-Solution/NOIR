namespace NOIR.Application.Features.Payments.Commands.RecordManualPayment;

/// <summary>
/// Handler for recording a manual/offline payment.
/// </summary>
public class RecordManualPaymentCommandHandler
{
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;
    private readonly IRepository<PaymentGateway, Guid> _gatewayRepository;
    private readonly IPaymentService _paymentService;
    private readonly IPaymentOperationLogger _operationLogger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public RecordManualPaymentCommandHandler(
        IRepository<Order, Guid> orderRepository,
        IRepository<PaymentTransaction, Guid> paymentRepository,
        IRepository<PaymentGateway, Guid> gatewayRepository,
        IPaymentService paymentService,
        IPaymentOperationLogger operationLogger,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _gatewayRepository = gatewayRepository;
        _paymentService = paymentService;
        _operationLogger = operationLogger;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PaymentTransactionDto>> Handle(
        RecordManualPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var orderSpec = new OrderByIdForUpdateSpec(command.OrderId);
        var order = await _orderRepository.FirstOrDefaultAsync(orderSpec, cancellationToken);

        if (order == null)
        {
            return Result.Failure<PaymentTransactionDto>(
                Error.NotFound("Order not found.", ErrorCodes.Order.NotFound));
        }

        // Only Pending or Confirmed orders can accept payment
        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
        {
            return Result.Failure<PaymentTransactionDto>(
                Error.Validation("OrderId", "Order status does not allow payment. Only Pending or Confirmed orders can accept payments.", ErrorCodes.Payment.OrderNotPayable));
        }

        var tenantId = _currentUser.TenantId;

        // Find a manual/COD gateway, or use a placeholder gateway ID
        var gatewaySpec = new PaymentGatewayByProviderSpec("manual");
        var gateway = await _gatewayRepository.FirstOrDefaultAsync(gatewaySpec, cancellationToken);

        // If no manual gateway, try COD gateway
        if (gateway == null)
        {
            var codGatewaySpec = new PaymentGatewayByProviderSpec("cod");
            gateway = await _gatewayRepository.FirstOrDefaultAsync(codGatewaySpec, cancellationToken);
        }

        var gatewayId = gateway?.Id ?? Guid.Empty;
        var providerName = gateway?.Provider ?? "manual";

        var transactionNumber = _paymentService.GenerateTransactionNumber();
        var idempotencyKey = Guid.NewGuid().ToString("N");

        var payment = PaymentTransaction.Create(
            transactionNumber,
            gatewayId,
            providerName,
            command.Amount,
            command.Currency,
            command.PaymentMethod,
            idempotencyKey,
            tenantId);

        payment.SetOrderId(command.OrderId);

        if (order.CustomerId.HasValue)
        {
            payment.SetCustomerId(order.CustomerId.Value);
        }

        // Mark as paid immediately.
        // Note: command.PaidAt is captured in operation log data below for audit purposes.
        // Domain entity sets PaidAt = UtcNow; to support backdating, add MarkAsPaid overload to domain.
        payment.MarkAsPaid(command.ReferenceNumber ?? "MANUAL");

        if (command.ReferenceNumber != null)
        {
            // Persist the reference number in the gateway response JSON field for traceability
            payment.SetGatewayResponse(JsonSerializer.Serialize(new { ReferenceNumber = command.ReferenceNumber }));
        }

        if (command.Notes != null)
        {
            payment.SetMetadataJson(JsonSerializer.Serialize(new { Notes = command.Notes }));
        }

        await _paymentRepository.AddAsync(payment, cancellationToken);

        // Update order status if payment is successful
        // For non-COD payments marked as Paid, confirm the order
        if (command.PaymentMethod != PaymentMethod.COD)
        {
            if (order.Status == OrderStatus.Pending)
            {
                order.Confirm();
            }
        }

        // Log the manual payment operation
        await _operationLogger.LogOperationAsync(
            PaymentOperationType.ManualPayment,
            providerName,
            success: true,
            transactionNumber: transactionNumber,
            paymentTransactionId: payment.Id,
            requestData: new
            {
                command.OrderId,
                command.Amount,
                command.Currency,
                command.PaymentMethod,
                command.ReferenceNumber,
                command.Notes,
                command.PaidAt
            },
            responseData: new { Status = "Paid", TransactionNumber = transactionNumber },
            cancellationToken: cancellationToken);

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
