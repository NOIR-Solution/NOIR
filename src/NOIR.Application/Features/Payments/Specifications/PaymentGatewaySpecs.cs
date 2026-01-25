namespace NOIR.Application.Features.Payments.Specifications;

/// <summary>
/// Get payment gateway by ID (read-only).
/// </summary>
public sealed class PaymentGatewayByIdSpec : Specification<PaymentGateway>
{
    public PaymentGatewayByIdSpec(Guid id)
    {
        Query.Where(g => g.Id == id)
             .TagWith("PaymentGatewayById");
    }
}

/// <summary>
/// Get payment gateway by ID for update (with tracking).
/// </summary>
public sealed class PaymentGatewayByIdForUpdateSpec : Specification<PaymentGateway>
{
    public PaymentGatewayByIdForUpdateSpec(Guid id)
    {
        Query.Where(g => g.Id == id)
             .AsTracking()
             .TagWith("PaymentGatewayByIdForUpdate");
    }
}

/// <summary>
/// Get all active payment gateways (for checkout display).
/// </summary>
public sealed class ActivePaymentGatewaysSpec : Specification<PaymentGateway>
{
    public ActivePaymentGatewaysSpec()
    {
        Query.Where(g => g.IsActive)
             .OrderBy(g => (object)g.SortOrder)
             .TagWith("ActivePaymentGateways");
    }
}

/// <summary>
/// Get payment gateway by provider name.
/// </summary>
public sealed class PaymentGatewayByProviderSpec : Specification<PaymentGateway>
{
    public PaymentGatewayByProviderSpec(string provider)
    {
        Query.Where(g => g.Provider == provider)
             .TagWith("PaymentGatewayByProvider");
    }
}

/// <summary>
/// Get all payment gateways (admin view).
/// </summary>
public sealed class PaymentGatewaysSpec : Specification<PaymentGateway>
{
    public PaymentGatewaysSpec()
    {
        Query.OrderBy(g => (object)g.SortOrder)
             .TagWith("GetPaymentGateways");
    }
}
