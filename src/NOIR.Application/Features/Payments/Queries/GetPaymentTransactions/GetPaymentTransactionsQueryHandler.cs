namespace NOIR.Application.Features.Payments.Queries.GetPaymentTransactions;

/// <summary>
/// Handler for getting a paginated list of payment transactions.
/// </summary>
public class GetPaymentTransactionsQueryHandler
{
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;

    public GetPaymentTransactionsQueryHandler(IRepository<PaymentTransaction, Guid> paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<PagedResult<PaymentTransactionListDto>>> Handle(
        GetPaymentTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        var spec = new PaymentTransactionsSpec(
            query.Search,
            query.Status,
            query.PaymentMethod,
            query.Provider,
            skip,
            query.PageSize);

        var payments = await _paymentRepository.ListAsync(spec, cancellationToken);

        // Count without pagination
        var countSpec = new PaymentTransactionsSpec(
            query.Search,
            query.Status,
            query.PaymentMethod,
            query.Provider);
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
