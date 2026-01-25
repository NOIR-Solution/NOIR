namespace NOIR.Application.Features.Payments.Specifications;

/// <summary>
/// Specification for fetching a PaymentOperationLog by ID with tracking for updates.
/// </summary>
public class PaymentOperationLogByIdForUpdateSpec : Specification<PaymentOperationLog>
{
    public PaymentOperationLogByIdForUpdateSpec(Guid id)
    {
        Query.Where(x => x.Id == id)
             .AsTracking()
             .TagWith("GetPaymentOperationLogByIdForUpdate");
    }
}

/// <summary>
/// Specification for fetching operation logs by transaction ID.
/// </summary>
public class PaymentOperationLogsByTransactionIdSpec : Specification<PaymentOperationLog>
{
    public PaymentOperationLogsByTransactionIdSpec(Guid transactionId)
    {
        Query.Where(x => x.PaymentTransactionId == transactionId)
             .OrderByDescending(x => x.CreatedAt)
             .TagWith("GetPaymentOperationLogsByTransactionId");
    }
}

/// <summary>
/// Specification for fetching operation logs by correlation ID.
/// </summary>
public class PaymentOperationLogsByCorrelationIdSpec : Specification<PaymentOperationLog>
{
    public PaymentOperationLogsByCorrelationIdSpec(string correlationId)
    {
        Query.Where(x => x.CorrelationId == correlationId)
             .OrderBy(x => x.CreatedAt)
             .TagWith("GetPaymentOperationLogsByCorrelationId");
    }
}

/// <summary>
/// Specification for searching operation logs with filters.
/// </summary>
public class PaymentOperationLogsSearchSpec : Specification<PaymentOperationLog>
{
    public PaymentOperationLogsSearchSpec(
        string? provider = null,
        PaymentOperationType? operationType = null,
        bool? success = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        string? transactionNumber = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        // AsNoTracking is the default, no need to call it explicitly

        if (!string.IsNullOrEmpty(provider))
        {
            Query.Where(x => x.Provider == provider);
        }

        if (operationType.HasValue)
        {
            Query.Where(x => x.OperationType == operationType.Value);
        }

        if (success.HasValue)
        {
            Query.Where(x => x.Success == success.Value);
        }

        if (fromDate.HasValue)
        {
            Query.Where(x => x.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            Query.Where(x => x.CreatedAt <= toDate.Value);
        }

        if (!string.IsNullOrEmpty(transactionNumber))
        {
            Query.Where(x => x.TransactionNumber != null &&
                           x.TransactionNumber.Contains(transactionNumber));
        }

        Query.OrderByDescending(x => x.CreatedAt)
             .Skip((pageNumber - 1) * pageSize)
             .Take(pageSize)
             .TagWith("SearchPaymentOperationLogs");
    }
}

/// <summary>
/// Specification for counting failed operations by provider.
/// </summary>
public class FailedOperationsByProviderSpec : Specification<PaymentOperationLog>
{
    public FailedOperationsByProviderSpec(string provider, DateTimeOffset since)
    {
        // AsNoTracking is the default, no need to call it explicitly
        Query.Where(x => x.Provider == provider &&
                        !x.Success &&
                        x.CreatedAt >= since)
             .TagWith("CountFailedOperationsByProvider");
    }
}
