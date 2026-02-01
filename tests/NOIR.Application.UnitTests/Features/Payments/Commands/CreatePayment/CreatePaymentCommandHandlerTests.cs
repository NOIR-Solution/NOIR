namespace NOIR.Application.UnitTests.Features.Payments.Commands.CreatePayment;

using NOIR.Application.Features.Payments.Commands.CreatePayment;
using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Specifications;

/// <summary>
/// Unit tests for CreatePaymentCommandHandler.
/// Tests payment creation scenarios with mocked dependencies.
/// </summary>
public class CreatePaymentCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentTransaction, Guid>> _paymentRepositoryMock;
    private readonly Mock<IRepository<PaymentGateway, Guid>> _gatewayRepositoryMock;
    private readonly Mock<IPaymentGatewayFactory> _gatewayFactoryMock;
    private readonly Mock<IPaymentGatewayProvider> _gatewayProviderMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<IPaymentOperationLogger> _operationLoggerMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IOptions<PaymentSettings>> _paymentSettingsMock;
    private readonly CreatePaymentCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestTransactionNumber = "PAY-20260131-001";
    private static readonly Guid TestOrderId = Guid.NewGuid();
    private static readonly Guid TestGatewayId = Guid.NewGuid();

    public CreatePaymentCommandHandlerTests()
    {
        _paymentRepositoryMock = new Mock<IRepository<PaymentTransaction, Guid>>();
        _gatewayRepositoryMock = new Mock<IRepository<PaymentGateway, Guid>>();
        _gatewayFactoryMock = new Mock<IPaymentGatewayFactory>();
        _gatewayProviderMock = new Mock<IPaymentGatewayProvider>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _operationLoggerMock = new Mock<IPaymentOperationLogger>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _paymentSettingsMock = new Mock<IOptions<PaymentSettings>>();

        // Default setup
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _paymentServiceMock.Setup(x => x.GenerateTransactionNumber()).Returns(TestTransactionNumber);
        _paymentSettingsMock.Setup(x => x.Value).Returns(new PaymentSettings { PaymentLinkExpiryMinutes = 30 });
        _operationLoggerMock.Setup(x => x.StartOperationAsync(
            It.IsAny<PaymentOperationType>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());

        _handler = new CreatePaymentCommandHandler(
            _paymentRepositoryMock.Object,
            _gatewayRepositoryMock.Object,
            _gatewayFactoryMock.Object,
            _paymentServiceMock.Object,
            _operationLoggerMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _paymentSettingsMock.Object);
    }

    private static CreatePaymentCommand CreateTestCommand(
        Guid? orderId = null,
        decimal amount = 500000m,
        string currency = "VND",
        PaymentMethod paymentMethod = PaymentMethod.CreditCard,
        string provider = "vnpay",
        string? idempotencyKey = null)
    {
        return new CreatePaymentCommand(
            orderId ?? TestOrderId,
            amount,
            currency,
            paymentMethod,
            provider,
            "https://example.com/return",
            idempotencyKey,
            new Dictionary<string, string> { { "orderId", (orderId ?? TestOrderId).ToString() } });
    }

    private static PaymentGateway CreateTestGateway(
        bool isActive = true,
        string provider = "vnpay")
    {
        var gateway = PaymentGateway.Create(
            provider,
            "VNPay",
            GatewayEnvironment.Sandbox,
            TestTenantId);

        // Use reflection to set Id
        typeof(PaymentGateway).GetProperty("Id")?.SetValue(gateway, TestGatewayId);

        gateway.Configure("encrypted-credentials", "webhook-secret");
        if (isActive)
        {
            gateway.Activate();
        }

        return gateway;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreatePayment()
    {
        // Arrange
        var command = CreateTestCommand();
        var gateway = CreateTestGateway();
        var gatewayTransactionId = "VNP123456789";

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        _gatewayFactoryMock
            .Setup(x => x.GetProvider("vnpay"))
            .Returns(_gatewayProviderMock.Object);

        _gatewayProviderMock
            .Setup(x => x.InitiatePaymentAsync(It.IsAny<PaymentInitiationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentInitiationResult(
                Success: true,
                GatewayTransactionId: gatewayTransactionId,
                PaymentUrl: null,
                RequiresAction: false));

        _paymentRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TransactionNumber.Should().Be(TestTransactionNumber);
        result.Value.Provider.Should().Be("vnpay");
        result.Value.Amount.Should().Be(500000m);
        result.Value.Status.Should().Be(PaymentStatus.Processing);

        _paymentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCodPaymentMethod_ShouldCreateCodPendingPayment()
    {
        // Arrange
        var command = CreateTestCommand(paymentMethod: PaymentMethod.COD, provider: "cod");
        var gateway = CreateTestGateway(provider: "cod");

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        _paymentRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentStatus.CodPending);
        result.Value.PaymentMethod.Should().Be(PaymentMethod.COD);

        // COD should not call the gateway provider
        _gatewayFactoryMock.Verify(x => x.GetProvider(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExistingIdempotencyKey_ShouldReturnExistingPayment()
    {
        // Arrange
        var idempotencyKey = "test-idempotency-key";
        var command = CreateTestCommand(idempotencyKey: idempotencyKey);

        var existingPayment = PaymentTransaction.Create(
            TestTransactionNumber,
            TestGatewayId,
            "vnpay",
            500000m,
            "VND",
            PaymentMethod.CreditCard,
            idempotencyKey,
            TestTenantId);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdempotencyKeySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPayment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TransactionNumber.Should().Be(TestTransactionNumber);

        // Should not create a new payment
        _paymentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenGatewayNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = CreateTestCommand();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be(ErrorCodes.Payment.GatewayNotFound);
        result.Error.Message.Should().Contain("vnpay");

        _paymentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenGatewayNotActive_ShouldReturnFailure()
    {
        // Arrange
        var command = CreateTestCommand();
        var inactiveGateway = CreateTestGateway(isActive: false);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveGateway);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.GatewayNotActive);

        _gatewayFactoryMock.Verify(x => x.GetProvider(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProviderNotConfigured_ShouldReturnFailure()
    {
        // Arrange
        var command = CreateTestCommand();
        var gateway = CreateTestGateway();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        _gatewayFactoryMock
            .Setup(x => x.GetProvider("vnpay"))
            .Returns((IPaymentGatewayProvider?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Failure);
        result.Error.Code.Should().Be(ErrorCodes.Payment.ProviderNotConfigured);
    }

    [Fact]
    public async Task Handle_WhenPaymentInitiationFails_ShouldMarkPaymentAsFailedAndReturnFailure()
    {
        // Arrange
        var command = CreateTestCommand();
        var gateway = CreateTestGateway();
        var errorMessage = "Insufficient funds";

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        _gatewayFactoryMock
            .Setup(x => x.GetProvider("vnpay"))
            .Returns(_gatewayProviderMock.Object);

        _gatewayProviderMock
            .Setup(x => x.InitiatePaymentAsync(It.IsAny<PaymentInitiationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentInitiationResult(
                Success: false,
                GatewayTransactionId: null,
                PaymentUrl: null,
                RequiresAction: false,
                ErrorMessage: errorMessage));

        _paymentRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Payment.InitiationFailed);
        result.Error.Message.Should().Be(errorMessage);

        // Verify failed payment was saved
        _paymentRepositoryMock.Verify(x => x.AddAsync(
            It.Is<PaymentTransaction>(p => p.Status == PaymentStatus.Failed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithRequiresAction_ShouldSetCorrectStatus()
    {
        // Arrange
        var command = CreateTestCommand();
        var gateway = CreateTestGateway();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        _gatewayFactoryMock
            .Setup(x => x.GetProvider("vnpay"))
            .Returns(_gatewayProviderMock.Object);

        _gatewayProviderMock
            .Setup(x => x.InitiatePaymentAsync(It.IsAny<PaymentInitiationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentInitiationResult(
                Success: true,
                GatewayTransactionId: "VNP123456789",
                PaymentUrl: null,
                RequiresAction: true));

        _paymentRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentStatus.RequiresAction);
    }

    #endregion
}
