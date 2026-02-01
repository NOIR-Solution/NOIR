namespace NOIR.Application.UnitTests.Features.Payments.Commands.ConfirmCodCollection;

using NOIR.Application.Features.Payments.Commands.ConfirmCodCollection;
using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Specifications;

/// <summary>
/// Unit tests for ConfirmCodCollectionCommandHandler.
/// Tests COD payment collection confirmation scenarios with mocked dependencies.
/// </summary>
public class ConfirmCodCollectionCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentTransaction, Guid>> _paymentRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPaymentHubContext> _paymentHubContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly ConfirmCodCollectionCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestTransactionNumber = "PAY-20260131-001";
    private const string TestUserId = "collector-user-123";
    private static readonly Guid TestPaymentId = Guid.NewGuid();
    private static readonly Guid TestGatewayId = Guid.NewGuid();

    public ConfirmCodCollectionCommandHandlerTests()
    {
        _paymentRepositoryMock = new Mock<IRepository<PaymentTransaction, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _paymentHubContextMock = new Mock<IPaymentHubContext>();
        _currentUserMock = new Mock<ICurrentUser>();

        // Default setup
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _paymentHubContextMock
            .Setup(x => x.SendCodCollectionUpdateAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new ConfirmCodCollectionCommandHandler(
            _paymentRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _paymentHubContextMock.Object,
            _currentUserMock.Object);
    }

    private static ConfirmCodCollectionCommand CreateTestCommand(
        Guid? paymentTransactionId = null,
        string? notes = "Collected cash from customer",
        string? userId = TestUserId)
    {
        return new ConfirmCodCollectionCommand(
            paymentTransactionId ?? TestPaymentId,
            notes) with { UserId = userId };
    }

    private static PaymentTransaction CreateTestCodPayment(
        PaymentStatus status = PaymentStatus.CodPending,
        PaymentMethod paymentMethod = PaymentMethod.COD)
    {
        var payment = PaymentTransaction.Create(
            TestTransactionNumber,
            TestGatewayId,
            "cod",
            500000m,
            "VND",
            paymentMethod,
            "idempotency-key",
            TestTenantId);

        typeof(PaymentTransaction).GetProperty("Id")?.SetValue(payment, TestPaymentId);

        // Set the status
        if (paymentMethod == PaymentMethod.COD && status == PaymentStatus.CodPending)
        {
            payment.MarkAsCodPending();
        }
        else if (status == PaymentStatus.CodCollected)
        {
            payment.MarkAsCodPending();
            payment.ConfirmCodCollection("previous-collector");
        }
        else if (status == PaymentStatus.Paid)
        {
            payment.MarkAsPaid("GW-TXN-001");
        }

        return payment;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithCodPendingPayment_ShouldConfirmCollectionSuccessfully()
    {
        // Arrange
        var command = CreateTestCommand();
        var codPayment = CreateTestCodPayment(PaymentStatus.CodPending);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(codPayment);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Status.Should().Be(PaymentStatus.CodCollected);
        result.Value.CodCollectorName.Should().Be(TestUserId);
        result.Value.CodCollectedAt.Should().NotBeNull();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _paymentHubContextMock.Verify(x => x.SendCodCollectionUpdateAsync(
            TestTenantId,
            TestPaymentId,
            TestTransactionNumber,
            TestUserId,
            500000m,
            "VND",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCollector_ShouldRecordCollectorName()
    {
        // Arrange
        var collectorName = "John Doe";
        var command = CreateTestCommand(userId: collectorName);
        var codPayment = CreateTestCodPayment(PaymentStatus.CodPending);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(codPayment);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CodCollectorName.Should().Be(collectorName);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenPaymentNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = CreateTestCommand();

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be(ErrorCodes.Payment.TransactionNotFound);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _paymentHubContextMock.Verify(x => x.SendCodCollectionUpdateAsync(
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<decimal>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPaymentNotCod_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand();
        var creditCardPayment = CreateTestCodPayment(paymentMethod: PaymentMethod.CreditCard);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(creditCardPayment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.NotCodPayment);
        result.Error.Message.Should().Contain("not a COD payment");
    }

    [Fact]
    public async Task Handle_WhenPaymentNotCodPending_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand();
        var paidPayment = PaymentTransaction.Create(
            TestTransactionNumber,
            TestGatewayId,
            "cod",
            500000m,
            "VND",
            PaymentMethod.COD,
            "idempotency-key",
            TestTenantId);
        typeof(PaymentTransaction).GetProperty("Id")?.SetValue(paidPayment, TestPaymentId);
        paidPayment.MarkAsPaid("GW-TXN-001"); // Not CodPending status

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paidPayment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.InvalidStatusTransition);
        result.Error.Message.Should().Contain("COD pending");
    }

    [Fact]
    public async Task Handle_WithEmptyUserId_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand(userId: "");
        var codPayment = CreateTestCodPayment(PaymentStatus.CodPending);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(codPayment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.InvalidRequesterId);
    }

    [Fact]
    public async Task Handle_WithNullUserId_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand(userId: null);
        var codPayment = CreateTestCodPayment(PaymentStatus.CodPending);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(codPayment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.InvalidRequesterId);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithNoTenantId_ShouldNotSendHubNotification()
    {
        // Arrange
        var command = CreateTestCommand();
        var codPayment = CreateTestCodPayment(PaymentStatus.CodPending);

        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(codPayment);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Hub notification should not be sent when tenantId is null
        _paymentHubContextMock.Verify(x => x.SendCodCollectionUpdateAsync(
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<decimal>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
