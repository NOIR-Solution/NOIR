namespace NOIR.Application.Features.Payments.Commands.RejectRefund;

/// <summary>
/// Handler for rejecting a refund request.
/// </summary>
public class RejectRefundCommandHandler
{
    private readonly IRepository<Refund, Guid> _refundRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RejectRefundCommandHandler(
        IRepository<Refund, Guid> refundRepository,
        IUnitOfWork unitOfWork)
    {
        _refundRepository = refundRepository;
        _unitOfWork = unitOfWork;
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

        refund.Reject(command.RejectionReason ?? "Rejected");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
