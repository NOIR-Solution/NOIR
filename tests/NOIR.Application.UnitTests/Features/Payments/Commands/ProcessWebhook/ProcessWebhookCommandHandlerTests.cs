namespace NOIR.Application.UnitTests.Features.Payments.Commands.ProcessWebhook;

using NOIR.Application.Features.Payments.Commands.ProcessWebhook;
using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Specifications;

/// <summary>
/// Unit tests for ProcessWebhookCommandHandler.
/// Tests webhook processing scenarios with mocked dependencies.
/// </summary>
public class ProcessWebhookCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentWebhookLog, Guid>> _webhookLogRepositoryMock;
    private readonly Mock<IRepository<PaymentTransaction, Guid>> _paymentRepositoryMock;
    private readonly Mock<IRepository<PaymentGateway, Guid>> _gatewayRepositoryMock;
    private readonly Mock<IPaymentGatewayFactory> _gatewayFactoryMock;
    private readonly Mock<IPaymentGatewayProvider> _gatewayProviderMock;
    private readonly Mock<IPaymentOperationLogger> _operationLoggerMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ProcessWebhookCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestProvider = "vnpay";
    private const string TestGatewayEventId = "EVT-VNP-001";
    private const string TestGatewayTransactionId = "VNP-TXN-001";
    private static readonly Guid TestGatewayId = Guid.NewGuid();
    private static readonly Guid TestPaymentId = Guid.NewGuid();

    public ProcessWebhookCommandHandlerTests()
    {
        _webhookLogRepositoryMock = new Mock<IRepository<PaymentWebhookLog, Guid>>();
        _paymentRepositoryMock = new Mock<IRepository<PaymentTransaction, Guid>>();
        _gatewayRepositoryMock = new Mock<IRepository<PaymentGateway, Guid>>();
        _gatewayFactoryMock = new Mock<IPaymentGatewayFactory>();
        _gatewayProviderMock = new Mock<IPaymentGatewayProvider>();
        _operationLoggerMock = new Mock<IPaymentOperationLogger>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        // Default setup
        _operationLoggerMock.Setup(x => x.StartOperationAsync(
            It.IsAny<PaymentOperationType>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());

        _handler = new ProcessWebhookCommandHandler(
            _webhookLogRepositoryMock.Object,
            _paymentRepositoryMock.Object,
            _gatewayRepositoryMock.Object,
            _gatewayFactoryMock.Object,
            _operationLoggerMock.Object,
            _unitOfWorkMock.Object);
    }

    private static ProcessWebhookCommand CreateTestCommand(
        string provider = TestProvider,
        string rawPayload = "{\"event\":\"payment.success\",\"transactionId\":\"VNP-TXN-001\"}",
        string? signature = "valid-signature",
        string? ipAddress = "192.168.1.1",
        Dictionary<string, string>? headers = null)
    {
        return new ProcessWebhookCommand(
            provider,
            rawPayload,
            signature,
            ipAddress,
            headers ?? new Dictionary<string, string> { { "X-Signature", "valid-signature" } });
    }

    private static PaymentGateway CreateTestGateway()
    {
        var gateway = PaymentGateway.Create(
            TestProvider,
            "VNPay",
            GatewayEnvironment.Sandbox,
            TestTenantId);

        typeof(PaymentGateway).GetProperty("Id")?.SetValue(gateway, TestGatewayId);
        gateway.Configure("encrypted-credentials", "webhook-secret");
        gateway.Activate();

        return gateway;
    }

    private static PaymentTransaction CreateTestPayment()
    {
        var payment = PaymentTransaction.Create(
            "PAY-20260131-001",
            TestGatewayId,
            TestProvider,
            500000m,
            "VND",
            PaymentMethod.CreditCard,
            "idempotency-key",
            TestTenantId);

        typeof(PaymentTransaction).GetProperty("Id")?.SetValue(payment, TestPaymentId);
        payment.SetGatewayTransactionId(TestGatewayTransactionId);
        payment.MarkAsProcessing();

        return payment;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidWebhook_ShouldProcessSuccessfully()
    {
        // Arrange
        var command = CreateTestCommand();
        var gateway = CreateTestGateway();
        var payment = CreateTestPayment();

        _gatewayFactoryMock
            .Setup(x => x.GetProvider(TestProvider))
            .Returns(_gatewayProviderMock.Object);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        _gatewayProviderMock
            .Setup(x => x.ValidateWebhookAsync(It.IsAny<WebhookPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebhookValidationResult(
                IsValid: true,
                GatewayTransactionId: TestGatewayTransactionId,
                EventType: "payment.success",
                PaymentStatus: PaymentStatus.Paid,
                GatewayEventId: TestGatewayEventId));

        _webhookLogRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<WebhookLogByGatewayEventIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentWebhookLog?)null);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByGatewayTransactionIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _webhookLogRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PaymentWebhookLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentWebhookLog entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Provider.Should().Be(TestProvider);
        result.Value.EventType.Should().Be("payment.success");
        result.Value.SignatureValid.Should().BeTrue();
        result.Value.ProcessingStatus.Should().Be(WebhookProcessingStatus.Processed);

        _webhookLogRepositoryMock.Verify(x => x.AddAsync(It.IsAny<PaymentWebhookLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateWebhook_ShouldReturnExistingLog()
    {
        // Arrange
        var command = CreateTestCommand();
        var gateway = CreateTestGateway();
        var existingLog = PaymentWebhookLog.Create(
            TestGatewayId,
            TestProvider,
            "payment.success",
            command.RawPayload,
            TestTenantId);
        existingLog.SetGatewayEventId(TestGatewayEventId);
        existingLog.MarkAsProcessed(TestPaymentId);

        _gatewayFactoryMock
            .Setup(x => x.GetProvider(TestProvider))
            .Returns(_gatewayProviderMock.Object);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        _gatewayProviderMock
            .Setup(x => x.ValidateWebhookAsync(It.IsAny<WebhookPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebhookValidationResult(
                IsValid: true,
                GatewayTransactionId: TestGatewayTransactionId,
                EventType: "payment.success",
                PaymentStatus: PaymentStatus.Paid,
                GatewayEventId: TestGatewayEventId));

        _webhookLogRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<WebhookLogByGatewayEventIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLog);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GatewayEventId.Should().Be(TestGatewayEventId);
        result.Value.ProcessingStatus.Should().Be(WebhookProcessingStatus.Processed);

        // Should not add new log when duplicate
        _webhookLogRepositoryMock.Verify(x => x.AddAsync(It.IsAny<PaymentWebhookLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenProviderNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = CreateTestCommand(provider: "unknown-provider");

        _gatewayFactoryMock
            .Setup(x => x.GetProvider("unknown-provider"))
            .Returns((IPaymentGatewayProvider?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be(ErrorCodes.Payment.ProviderNotConfigured);

        _gatewayRepositoryMock.Verify(x => x.FirstOrDefaultAsync(
            It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenGatewayNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = CreateTestCommand();

        _gatewayFactoryMock
            .Setup(x => x.GetProvider(TestProvider))
            .Returns(_gatewayProviderMock.Object);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be(ErrorCodes.Payment.GatewayNotFound);
    }

    [Fact]
    public async Task Handle_WithInvalidSignature_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand(signature: "invalid-signature");
        var gateway = CreateTestGateway();

        _gatewayFactoryMock
            .Setup(x => x.GetProvider(TestProvider))
            .Returns(_gatewayProviderMock.Object);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        _gatewayProviderMock
            .Setup(x => x.ValidateWebhookAsync(It.IsAny<WebhookPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebhookValidationResult(
                IsValid: false,
                GatewayTransactionId: null,
                EventType: "payment.success",
                PaymentStatus: null,
                GatewayEventId: null));

        _webhookLogRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<WebhookLogByGatewayEventIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentWebhookLog?)null);

        _webhookLogRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PaymentWebhookLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentWebhookLog entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.InvalidWebhookSignature);

        // Should still save the failed webhook log
        _webhookLogRepositoryMock.Verify(x => x.AddAsync(
            It.Is<PaymentWebhookLog>(w => w.ProcessingStatus == WebhookProcessingStatus.Failed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithPaymentNotFound_ShouldStillProcessWebhook()
    {
        // Arrange
        var command = CreateTestCommand();
        var gateway = CreateTestGateway();

        _gatewayFactoryMock
            .Setup(x => x.GetProvider(TestProvider))
            .Returns(_gatewayProviderMock.Object);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        _gatewayProviderMock
            .Setup(x => x.ValidateWebhookAsync(It.IsAny<WebhookPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebhookValidationResult(
                IsValid: true,
                GatewayTransactionId: "UNKNOWN-TXN",
                EventType: "payment.success",
                PaymentStatus: PaymentStatus.Paid,
                GatewayEventId: TestGatewayEventId));

        _webhookLogRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<WebhookLogByGatewayEventIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentWebhookLog?)null);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByGatewayTransactionIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        _webhookLogRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PaymentWebhookLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentWebhookLog entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentTransactionId.Should().BeNull();
        result.Value.ProcessingStatus.Should().Be(WebhookProcessingStatus.Processed);
    }

    #endregion
}
