namespace NOIR.Application.Features.Payments.Specifications;

/// <summary>
/// Get payment transaction by ID (read-only).
/// </summary>
public sealed class PaymentTransactionByIdSpec : Specification<PaymentTransaction>
{
    public PaymentTransactionByIdSpec(Guid id)
    {
        Query.Where(t => t.Id == id)
             .Include(t => t.Gateway!)
             .TagWith("PaymentTransactionById");
    }
}

/// <summary>
/// Get payment transaction by ID for update (with tracking).
/// </summary>
public sealed class PaymentTransactionByIdForUpdateSpec : Specification<PaymentTransaction>
{
    public PaymentTransactionByIdForUpdateSpec(Guid id)
    {
        Query.Where(t => t.Id == id)
             .AsTracking()
             .TagWith("PaymentTransactionByIdForUpdate");
    }
}

/// <summary>
/// Get payment transaction by transaction number.
/// </summary>
public sealed class PaymentTransactionByNumberSpec : Specification<PaymentTransaction>
{
    public PaymentTransactionByNumberSpec(string transactionNumber)
    {
        Query.Where(t => t.TransactionNumber == transactionNumber)
             .Include(t => t.Gateway!)
             .TagWith("PaymentTransactionByNumber");
    }
}

/// <summary>
/// Get all payments for a specific order.
/// </summary>
public sealed class PaymentTransactionsByOrderSpec : Specification<PaymentTransaction>
{
    public PaymentTransactionsByOrderSpec(Guid orderId)
    {
        Query.Where(t => t.OrderId == orderId)
             .OrderByDescending(t => (object)t.CreatedAt)
             .TagWith("PaymentTransactionsByOrder");
    }
}

/// <summary>
/// Get paginated list of payment transactions with filtering.
/// </summary>
public sealed class PaymentTransactionsSpec : Specification<PaymentTransaction>
{
    public PaymentTransactionsSpec(
        string? search = null,
        PaymentStatus? status = null,
        PaymentMethod? paymentMethod = null,
        string? provider = null,
        int? skip = null,
        int? take = null)
    {
        Query.Where(t => string.IsNullOrEmpty(search) ||
                         t.TransactionNumber.Contains(search) ||
                         (t.GatewayTransactionId != null && t.GatewayTransactionId.Contains(search)))
             .Where(t => status == null || t.Status == status)
             .Where(t => paymentMethod == null || t.PaymentMethod == paymentMethod)
             .Where(t => string.IsNullOrEmpty(provider) || t.Provider == provider)
             .Include(t => t.Gateway!)
             .OrderByDescending(t => (object)t.CreatedAt)
             .TagWith("GetPaymentTransactions");

        if (skip.HasValue)
            Query.Skip(skip.Value);
        if (take.HasValue)
            Query.Take(take.Value);
    }
}

/// <summary>
/// Get pending payments that have expired.
/// </summary>
public sealed class ExpiredPaymentsSpec : Specification<PaymentTransaction>
{
    public ExpiredPaymentsSpec()
    {
        Query.Where(t => t.Status == PaymentStatus.Pending || t.Status == PaymentStatus.RequiresAction)
             .Where(t => t.ExpiresAt != null && t.ExpiresAt < DateTimeOffset.UtcNow)
             .AsTracking()
             .TagWith("ExpiredPayments");
    }
}

/// <summary>
/// Get pending COD payments awaiting collection.
/// </summary>
public sealed class PendingCodPaymentsSpec : Specification<PaymentTransaction>
{
    public PendingCodPaymentsSpec(int? skip = null, int? take = null)
    {
        Query.Where(t => t.PaymentMethod == PaymentMethod.COD)
             .Where(t => t.Status == PaymentStatus.CodPending)
             .OrderBy(t => (object)t.CreatedAt)
             .TagWith("PendingCodPayments");

        if (skip.HasValue)
            Query.Skip(skip.Value);
        if (take.HasValue)
            Query.Take(take.Value);
    }
}

/// <summary>
/// Get payment transaction by idempotency key.
/// </summary>
public sealed class PaymentTransactionByIdempotencyKeySpec : Specification<PaymentTransaction>
{
    public PaymentTransactionByIdempotencyKeySpec(string idempotencyKey)
    {
        Query.Where(t => t.IdempotencyKey == idempotencyKey)
             .TagWith("PaymentTransactionByIdempotencyKey");
    }
}


/// <summary>
/// Get payment transaction by gateway transaction ID.
/// </summary>
public sealed class PaymentTransactionByGatewayTransactionIdSpec : Specification<PaymentTransaction>
{
    public PaymentTransactionByGatewayTransactionIdSpec(string gatewayTransactionId)
    {
        Query.Where(t => t.GatewayTransactionId == gatewayTransactionId)
             .AsTracking()
             .TagWith("PaymentTransactionByGatewayTransactionId");
    }
}
