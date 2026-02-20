namespace NOIR.Application.Features.Payments.Commands.RejectRefund;

/// <summary>
/// Handler for rejecting a refund request.
/// </summary>
public class RejectRefundCommandHandler
{
    private readonly IRepository<Refund, Guid> _refundRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentHubContext _paymentHubContext;

    public RejectRefundCommandHandler(
        IRepository<Refund, Guid> refundRepository,
        IUnitOfWork unitOfWork,
        IPaymentHubContext paymentHubContext)
    {
        _refundRepository = refundRepository;
        _unitOfWork = unitOfWork;
        _paymentHubContext = paymentHubContext;
    }

    public async Task<Result<RefundDto>> Handle(
        RejectRefundCommand command,
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
                Error.Validation("Status", "Only pending refunds can be rejected.", ErrorCodes.Payment.InvalidRefundStatus));
        }

        // Validate rejector ID
        if (string.IsNullOrEmpty(command.UserId))
        {
            return Result.Failure<RefundDto>(
                Error.Validation("UserId", "Invalid user ID.", ErrorCodes.Payment.InvalidRequesterId));
        }

        var rejectionReason = command.RejectionReason ?? "Rejected";
        refund.Reject(rejectionReason);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<RefundDto>(
                Error.Conflict("This refund was modified by another user. Please refresh and try again.", ErrorCodes.Payment.ConcurrencyConflict));
        }

        // Send real-time notification for refund rejection
        await _paymentHubContext.SendRefundStatusUpdateAsync(
            refund.Id,
            refund.RefundNumber,
            refund.PaymentTransactionId,
            refund.Status.ToString(),
            refund.Amount,
            rejectionReason,
            cancellationToken);

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
