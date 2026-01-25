namespace NOIR.Infrastructure.Services.Payment;

/// <summary>
/// High-level payment service for orchestrating payment operations.
/// </summary>
public class PaymentService : IPaymentService, IScopedService
{
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;
    private readonly IRepository<PaymentGateway, Guid> _gatewayRepository;
    private readonly IRepository<Refund, Guid> _refundRepository;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOptions<PaymentSettings> _paymentSettings;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IRepository<PaymentTransaction, Guid> paymentRepository,
        IRepository<PaymentGateway, Guid> gatewayRepository,
        IRepository<Refund, Guid> refundRepository,
        IPaymentGatewayFactory gatewayFactory,
        IUnitOfWork unitOfWork,
        IOptions<PaymentSettings> paymentSettings,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _gatewayRepository = gatewayRepository;
        _refundRepository = refundRepository;
        _gatewayFactory = gatewayFactory;
        _unitOfWork = unitOfWork;
        _paymentSettings = paymentSettings;
        _logger = logger;
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
        // Single query with tracking and payment transaction included
        var refundSpec = new RefundByIdForUpdateSpec(refundId);
        var refund = await _refundRepository.FirstOrDefaultAsync(refundSpec, cancellationToken);

        if (refund == null)
        {
            return new RefundResult(false, null, "Refund not found");
        }

        if (refund.Status != RefundStatus.Approved)
        {
            return new RefundResult(false, null, $"Refund must be in Approved status to process. Current: {refund.Status}");
        }

        var payment = refund.PaymentTransaction;
        if (payment == null)
        {
            return new RefundResult(false, null, "Associated payment transaction not found");
        }

        if (string.IsNullOrEmpty(payment.GatewayTransactionId))
        {
            return new RefundResult(false, null, "Payment has no gateway transaction ID");
        }

        // Get the payment provider
        var gatewayProvider = await _gatewayFactory.GetProviderWithCredentialsAsync(
            payment.Provider, cancellationToken);

        if (gatewayProvider == null)
        {
            return new RefundResult(false, null, $"Payment provider '{payment.Provider}' not available");
        }

        // Mark as processing
        refund.MarkAsProcessing();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Processing refund {RefundNumber} for payment {TransactionNumber}, Amount: {Amount} {Currency}",
            refund.RefundNumber, payment.TransactionNumber, refund.Amount, refund.Currency);

        // Call the gateway with exception handling to ensure state is updated on failure
        try
        {
            var refundRequest = new RefundRequest(
                GatewayTransactionId: payment.GatewayTransactionId,
                RefundNumber: refund.RefundNumber,
                Amount: refund.Amount,
                Currency: refund.Currency,
                Reason: refund.ReasonDetail);

            var result = await gatewayProvider.RefundAsync(refundRequest, cancellationToken);

            if (result.Success && !string.IsNullOrEmpty(result.GatewayRefundId))
            {
                refund.Complete(result.GatewayRefundId);
                _logger.LogInformation(
                    "Refund {RefundNumber} completed successfully. Gateway RefundId: {GatewayRefundId}",
                    refund.RefundNumber, result.GatewayRefundId);
            }
            else
            {
                refund.MarkAsFailed(result.ErrorMessage ?? "Unknown error");
                _logger.LogWarning(
                    "Refund {RefundNumber} failed: {ErrorMessage}",
                    refund.RefundNumber, result.ErrorMessage);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            refund.MarkAsFailed($"Gateway exception: {ex.Message}");
            _logger.LogError(ex,
                "Exception occurred while processing refund {RefundNumber}",
                refund.RefundNumber);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new RefundResult(false, null, $"Gateway communication error: {ex.Message}");
        }
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
