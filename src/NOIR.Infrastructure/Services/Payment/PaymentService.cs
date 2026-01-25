namespace NOIR.Infrastructure.Services.Payment;

/// <summary>
/// High-level payment service for orchestrating payment operations.
/// </summary>
public class PaymentService : IPaymentService, IScopedService
{
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;
    private readonly IRepository<PaymentGateway, Guid> _gatewayRepository;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOptions<PaymentSettings> _paymentSettings;

    public PaymentService(
        IRepository<PaymentTransaction, Guid> paymentRepository,
        IRepository<PaymentGateway, Guid> gatewayRepository,
        IPaymentGatewayFactory gatewayFactory,
        IUnitOfWork unitOfWork,
        IOptions<PaymentSettings> paymentSettings)
    {
        _paymentRepository = paymentRepository;
        _gatewayRepository = gatewayRepository;
        _gatewayFactory = gatewayFactory;
        _unitOfWork = unitOfWork;
        _paymentSettings = paymentSettings;
    }

    public string GenerateTransactionNumber()
    {
        // Format: TXN-{timestamp}-{random}
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"TXN-{timestamp}-{random}";
    }

    public string GenerateRefundNumber()
    {
        // Format: RFN-{timestamp}-{random}
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"RFN-{timestamp}-{random}";
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(
        Guid paymentTransactionId,
        CancellationToken cancellationToken = default)
    {
        var spec = new PaymentTransactionByIdSpec(paymentTransactionId);
        var payment = await _paymentRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (payment == null)
        {
            return new PaymentStatusResult(
                false,
                PaymentStatus.Failed,
                null,
                "Payment transaction not found");
        }

        // If payment is already in a final state, return it
        if (payment.Status is PaymentStatus.Paid or PaymentStatus.Failed or
            PaymentStatus.Cancelled or PaymentStatus.Expired or PaymentStatus.Refunded)
        {
            return new PaymentStatusResult(
                true,
                payment.Status,
                payment.GatewayTransactionId,
                null);
        }

        // Query gateway for current status
        var gatewayProvider = await _gatewayFactory.GetProviderWithCredentialsAsync(payment.Provider, cancellationToken);
        if (gatewayProvider == null || string.IsNullOrEmpty(payment.GatewayTransactionId))
        {
            return new PaymentStatusResult(
                true,
                payment.Status,
                payment.GatewayTransactionId,
                null);
        }

        var statusResult = await gatewayProvider.GetPaymentStatusAsync(payment.GatewayTransactionId, cancellationToken);
        return statusResult;
    }

    public async Task<RefundResult> ProcessRefundAsync(
        Guid refundId,
        CancellationToken cancellationToken = default)
    {
        // Implementation would process an approved refund through the gateway
        // This is a placeholder - actual implementation depends on the gateway APIs
        await Task.CompletedTask;

        return new RefundResult(
            false,
            null,
            "Refund processing not implemented yet");
    }

    public async Task ExpireStalePaymentsAsync(CancellationToken cancellationToken = default)
    {
        var spec = new ExpiredPaymentsSpec();
        var expiredPayments = await _paymentRepository.ListAsync(spec, cancellationToken);

        foreach (var payment in expiredPayments)
        {
            payment.MarkAsExpired();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsGatewayHealthyAsync(string provider, CancellationToken cancellationToken = default)
    {
        var spec = new PaymentGatewayByProviderSpec(provider);
        var gateway = await _gatewayRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (gateway == null || !gateway.IsActive)
        {
            return false;
        }

        return gateway.HealthStatus == GatewayHealthStatus.Healthy ||
               gateway.HealthStatus == GatewayHealthStatus.Unknown;
    }
}
