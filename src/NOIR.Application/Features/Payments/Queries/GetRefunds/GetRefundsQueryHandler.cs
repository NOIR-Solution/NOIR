namespace NOIR.Application.Features.Payments.Queries.GetRefunds;

/// <summary>
/// Handler for getting refunds for a payment transaction.
/// </summary>
public class GetRefundsQueryHandler
{
    private readonly IRepository<Refund, Guid> _refundRepository;

    public GetRefundsQueryHandler(IRepository<Refund, Guid> refundRepository)
    {
        _refundRepository = refundRepository;
    }

    public async Task<Result<List<RefundDto>>> Handle(
        GetRefundsQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new RefundsByPaymentSpec(query.PaymentTransactionId);
        var refunds = await _refundRepository.ListAsync(spec, cancellationToken);

        var items = refunds.Select(r => new RefundDto(
            r.Id,
            r.RefundNumber,
            r.PaymentTransactionId,
            r.GatewayRefundId,
            r.Amount,
            r.Currency,
            r.Status,
            r.Reason,
            r.ReasonDetail,
            r.RequestedBy,
            r.ApprovedBy,
            r.ProcessedAt,
            r.CreatedAt)).ToList();

        return Result.Success(items);
    }
}
