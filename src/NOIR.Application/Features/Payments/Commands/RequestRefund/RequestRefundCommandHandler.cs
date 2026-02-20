namespace NOIR.Application.Features.Payments.Commands.RequestRefund;

/// <summary>
/// Handler for requesting a refund.
/// </summary>
public class RequestRefundCommandHandler
{
    private readonly IRepository<Refund, Guid> _refundRepository;
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;
    private readonly IPaymentService _paymentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IOptions<PaymentSettings> _paymentSettings;

    public RequestRefundCommandHandler(
        IRepository<Refund, Guid> refundRepository,
        IRepository<PaymentTransaction, Guid> paymentRepository,
        IPaymentService paymentService,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IOptions<PaymentSettings> paymentSettings)
    {
        _refundRepository = refundRepository;
        _paymentRepository = paymentRepository;
        _paymentService = paymentService;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _paymentSettings = paymentSettings;
    }

    public async Task<Result<RefundDto>> Handle(
        RequestRefundCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Get the payment
        var paymentSpec = new PaymentTransactionByIdSpec(command.PaymentTransactionId);
        var payment = await _paymentRepository.FirstOrDefaultAsync(paymentSpec, cancellationToken);

        if (payment == null)
        {
            return Result.Failure<RefundDto>(
                Error.NotFound("Payment transaction not found.", ErrorCodes.Payment.TransactionNotFound));
        }

        // Validate payment status
        if (payment.Status != PaymentStatus.Paid && payment.Status != PaymentStatus.PartialRefund)
        {
            return Result.Failure<RefundDto>(
                Error.Validation("Status", "Only paid payments can be refunded.", ErrorCodes.Payment.InvalidStatusTransition));
        }

        // Validate refund window
        var settings = _paymentSettings.Value;
        var maxRefundDate = payment.PaidAt?.AddDays(settings.MaxRefundDays) ?? DateTimeOffset.UtcNow;
        if (DateTimeOffset.UtcNow > maxRefundDate)
        {
            return Result.Failure<RefundDto>(
                Error.Validation("RefundWindow", $"Refunds must be requested within {settings.MaxRefundDays} days of payment.", ErrorCodes.Payment.RefundWindowExpired));
        }

        // Validate refund amount
        var existingRefundsSpec = new RefundsByPaymentSpec(command.PaymentTransactionId);
        var existingRefunds = await _refundRepository.ListAsync(existingRefundsSpec, cancellationToken);
        var totalRefunded = existingRefunds
            .Where(r => r.Status == RefundStatus.Completed || r.Status == RefundStatus.Pending || r.Status == RefundStatus.Approved || r.Status == RefundStatus.Processing)
            .Sum(r => r.Amount);

        if (command.Amount + totalRefunded > payment.Amount)
        {
            return Result.Failure<RefundDto>(
                Error.Validation("Amount", "Refund amount exceeds available balance.", ErrorCodes.Payment.RefundAmountExceedsBalance));
        }

        // Validate requester
        if (string.IsNullOrEmpty(command.UserId))
        {
            return Result.Failure<RefundDto>(
                Error.Validation("UserId", "Invalid requester ID.", ErrorCodes.Payment.InvalidRequesterId));
        }

        // Create refund request
        var refundNumber = _paymentService.GenerateRefundNumber();
        var refund = Refund.Create(
            refundNumber,
            command.PaymentTransactionId,
            command.Amount,
            payment.Currency,
            command.Reason,
            command.Notes,
            command.UserId,
            tenantId);

        // Auto-approve if under threshold and approval not required
        if (!settings.RequireRefundApproval || command.Amount <= settings.RefundApprovalThreshold)
        {
            refund.Approve(command.UserId);
        }

        await _refundRepository.AddAsync(refund, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // If auto-approved, process the refund through the gateway
        if (refund.Status == RefundStatus.Approved)
        {
            // ProcessRefundAsync updates the refund status internally (Completed/Failed)
            await _paymentService.ProcessRefundAsync(refund.Id, cancellationToken);

            // Re-fetch to return the updated status after gateway processing
            var updatedRefund = await _refundRepository.FirstOrDefaultAsync(
                new RefundByIdSpec(refund.Id), cancellationToken);

            return Result.Success(MapToDto(updatedRefund ?? refund));
        }

        return Result.Success(MapToDto(refund));
    }

    private static RefundDto MapToDto(Refund refund)
    {
        return new RefundDto(
            refund.Id,
            refund.RefundNumber,
            refund.PaymentTransactionId,
            refund.GatewayRefundId,
            refund.Amount,
            refund.Currency,
            refund.Status,
            refund.Reason,
            refund.ReasonDetail,
            refund.RequestedBy,
            refund.ApprovedBy,
            refund.ProcessedAt,
            refund.CreatedAt);
    }
}
