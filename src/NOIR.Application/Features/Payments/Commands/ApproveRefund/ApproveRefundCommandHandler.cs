namespace NOIR.Application.Features.Payments.Commands.ApproveRefund;

/// <summary>
/// Handler for approving a refund request.
/// </summary>
public class ApproveRefundCommandHandler
{
    private readonly IRepository<Refund, Guid> _refundRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentHubContext _paymentHubContext;
    private readonly IPaymentService _paymentService;

    public ApproveRefundCommandHandler(
        IRepository<Refund, Guid> refundRepository,
        IUnitOfWork unitOfWork,
        IPaymentHubContext paymentHubContext,
        IPaymentService paymentService)
    {
        _refundRepository = refundRepository;
        _unitOfWork = unitOfWork;
        _paymentHubContext = paymentHubContext;
        _paymentService = paymentService;
    }

    public async Task<Result<RefundDto>> Handle(
        ApproveRefundCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new RefundByIdForUpdateSpec(command.RefundId);
        var refund = await _refundRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (refund == null)
        {
            return Result.Failure<RefundDto>(
                Error.NotFound("Refund not found.", ErrorCodes.Payment.RefundNotFound));
        }

        if (refund.Status != RefundStatus.Pending)
        {
            return Result.Failure<RefundDto>(
                Error.Validation("Status", "Only pending refunds can be approved.", ErrorCodes.Payment.InvalidRefundStatus));
        }

        // Validate approver ID
        if (string.IsNullOrEmpty(command.UserId))
        {
            return Result.Failure<RefundDto>(
                Error.Validation("UserId", "Invalid approver ID.", ErrorCodes.Payment.InvalidRequesterId));
        }

        refund.Approve(command.UserId);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<RefundDto>(
                Error.Conflict("This refund was modified by another user. Please refresh and try again.", ErrorCodes.Payment.ConcurrencyConflict));
        }

        // Send real-time notification for refund status change
        await _paymentHubContext.SendRefundStatusUpdateAsync(
            refund.Id,
            refund.RefundNumber,
            refund.PaymentTransactionId,
            refund.Status.ToString(),
            refund.Amount,
            "Refund approved",
            cancellationToken);

        // Process the refund through the payment gateway
        var refundResult = await _paymentService.ProcessRefundAsync(refund.Id, cancellationToken);

        // Re-fetch the refund to get the updated status after processing
        var updatedRefund = await _refundRepository.FirstOrDefaultAsync(
            new RefundByIdSpec(refund.Id), cancellationToken);

        var dto = MapToDto(updatedRefund ?? refund);

        // Return success with the updated DTO - the refund status (Completed/Failed)
        // reflects the gateway result. The approval itself succeeded; gateway failure
        // is visible through the refund's Failed status for operational follow-up.
        return Result.Success(dto);
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
