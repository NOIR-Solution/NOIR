namespace NOIR.Application.Features.Payments.Queries.GetPendingCodPayments;

/// <summary>
/// Handler for getting pending COD payments.
/// </summary>
public class GetPendingCodPaymentsQueryHandler
{
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;

    public GetPendingCodPaymentsQueryHandler(IRepository<PaymentTransaction, Guid> paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<PagedResult<PaymentTransactionListDto>>> Handle(
        GetPendingCodPaymentsQuery query,
        CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        var spec = new PendingCodPaymentsSpec(skip, query.PageSize);
        var payments = await _paymentRepository.ListAsync(spec, cancellationToken);

        // Count without pagination
        var countSpec = new PendingCodPaymentsSpec();
        var totalCount = await _paymentRepository.CountAsync(countSpec, cancellationToken);

        var items = payments.Select(p => new PaymentTransactionListDto(
            p.Id,
            p.TransactionNumber,
            p.Provider,
            p.Amount,
            p.Currency,
            p.Status,
            p.PaymentMethod,
            p.PaidAt,
            p.CreatedAt)).ToList();

        var result = PagedResult<PaymentTransactionListDto>.Create(
            items,
            totalCount,
            query.Page - 1,  // Convert 1-based page to 0-based pageIndex
            query.PageSize);

        return Result.Success(result);
    }
}
