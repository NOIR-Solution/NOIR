namespace NOIR.Application.UnitTests.Features.Payments.Commands.RequestRefund;

using NOIR.Application.Features.Payments.Commands.RequestRefund;
using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Specifications;

/// <summary>
/// Unit tests for RequestRefundCommandHandler.
/// Tests refund request scenarios with mocked dependencies.
/// </summary>
public class RequestRefundCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Refund, Guid>> _refundRepositoryMock;
    private readonly Mock<IRepository<PaymentTransaction, Guid>> _paymentRepositoryMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IOptions<PaymentSettings>> _paymentSettingsMock;
    private readonly RequestRefundCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestRefundNumber = "REF-20260131-001";
    private const string TestUserId = "user-123";
    private static readonly Guid TestPaymentTransactionId = Guid.NewGuid();
    private static readonly Guid TestGatewayId = Guid.NewGuid();

    public RequestRefundCommandHandlerTests()
    {
        _refundRepositoryMock = new Mock<IRepository<Refund, Guid>>();
        _paymentRepositoryMock = new Mock<IRepository<PaymentTransaction, Guid>>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _paymentSettingsMock = new Mock<IOptions<PaymentSettings>>();

        // Default setup
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _paymentServiceMock.Setup(x => x.GenerateRefundNumber()).Returns(TestRefundNumber);
        _paymentServiceMock
            .Setup(x => x.ProcessRefundAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefundResult(true, null, null));
        _paymentSettingsMock.Setup(x => x.Value).Returns(new PaymentSettings
        {
            MaxRefundDays = 30,
            RequireRefundApproval = true,
            RefundApprovalThreshold = 500000m
        });

        _handler = new RequestRefundCommandHandler(
            _refundRepositoryMock.Object,
            _paymentRepositoryMock.Object,
            _paymentServiceMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _paymentSettingsMock.Object);
    }

    private static RequestRefundCommand CreateTestCommand(
        Guid? paymentTransactionId = null,
        decimal amount = 100000m,
        RefundReason reason = RefundReason.CustomerRequest,
        string? notes = "Customer requested refund",
        string? userId = TestUserId)
    {
        return new RequestRefundCommand(
            paymentTransactionId ?? TestPaymentTransactionId,
            amount,
            reason,
            notes) with { UserId = userId };
    }

    private static PaymentTransaction CreateTestPayment(
        PaymentStatus status = PaymentStatus.Paid,
        decimal amount = 500000m,
        DateTimeOffset? paidAt = null)
    {
        var payment = PaymentTransaction.Create(
            "PAY-20260131-001",
            TestGatewayId,
            "vnpay",
            amount,
            "VND",
            PaymentMethod.CreditCard,
            "idempotency-key",
            TestTenantId);

        typeof(PaymentTransaction).GetProperty("Id")?.SetValue(payment, TestPaymentTransactionId);

        // Set the status
        if (status == PaymentStatus.Paid)
        {
            payment.MarkAsPaid("GW-TXN-001");
            // Set PaidAt if provided
            if (paidAt.HasValue)
            {
                typeof(PaymentTransaction).GetProperty("PaidAt")?.SetValue(payment, paidAt.Value);
            }
        }
        else if (status == PaymentStatus.PartialRefund)
        {
            payment.MarkAsPaid("GW-TXN-001");
            // Note: PartialRefund status would typically be set after a partial refund
        }
        else if (status == PaymentStatus.Pending)
        {
            // Default pending status
        }
        else if (status == PaymentStatus.Failed)
        {
            payment.MarkAsFailed("Test failure");
        }

        return payment;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCreateRefund()
    {
        // Arrange
        var command = CreateTestCommand();
        var paidPayment = CreateTestPayment(PaymentStatus.Paid, paidAt: DateTimeOffset.UtcNow.AddDays(-5));

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paidPayment);

        _refundRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<RefundsByPaymentSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Refund>());

        _refundRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Refund>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Refund entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.RefundNumber.Should().Be(TestRefundNumber);
        result.Value.Amount.Should().Be(100000m);
        result.Value.Reason.Should().Be(RefundReason.CustomerRequest);
        result.Value.RequestedBy.Should().Be(TestUserId);

        _refundRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Refund>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAmountBelowThreshold_ShouldAutoApprove()
    {
        // Arrange
        var command = CreateTestCommand(amount: 100000m); // Below 500000 threshold
        var paidPayment = CreateTestPayment(PaymentStatus.Paid, paidAt: DateTimeOffset.UtcNow.AddDays(-5));

        _paymentSettingsMock.Setup(x => x.Value).Returns(new PaymentSettings
        {
            MaxRefundDays = 30,
            RequireRefundApproval = false, // Auto-approve enabled
            RefundApprovalThreshold = 500000m
        });

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paidPayment);

        _refundRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<RefundsByPaymentSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Refund>());

        _refundRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Refund>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Refund entity, CancellationToken _) => entity);

        // Re-fetch after processing uses RefundByIdSpec (read-only) - returns null so handler uses in-memory refund
        _refundRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<RefundByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Refund?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(RefundStatus.Approved);
        result.Value.ApprovedBy.Should().Be(TestUserId);
        _paymentServiceMock.Verify(x => x.ProcessRefundAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenPaymentNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = CreateTestCommand();

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be(ErrorCodes.Payment.TransactionNotFound);

        _refundRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Refund>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPaymentNotPaid_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand();
        var pendingPayment = CreateTestPayment(PaymentStatus.Pending);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingPayment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.InvalidStatusTransition);
        result.Error.Message.Should().Contain("paid");
    }

    [Fact]
    public async Task Handle_WhenRefundWindowExpired_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand();
        var oldPayment = CreateTestPayment(PaymentStatus.Paid, paidAt: DateTimeOffset.UtcNow.AddDays(-45)); // Older than 30 days

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldPayment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.RefundWindowExpired);
        result.Error.Message.Should().Contain("30 days");
    }

    [Fact]
    public async Task Handle_WhenRefundAmountExceedsBalance_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand(amount: 600000m); // More than payment amount of 500000
        var paidPayment = CreateTestPayment(PaymentStatus.Paid, amount: 500000m, paidAt: DateTimeOffset.UtcNow.AddDays(-5));

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paidPayment);

        _refundRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<RefundsByPaymentSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Refund>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.RefundAmountExceedsBalance);
    }

    [Fact]
    public async Task Handle_WhenExistingRefundsExceedBalance_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand(amount: 200000m);
        var paidPayment = CreateTestPayment(PaymentStatus.Paid, amount: 500000m, paidAt: DateTimeOffset.UtcNow.AddDays(-5));

        // Existing refunds totaling 400000
        var existingRefund = Refund.Create(
            "REF-OLD-001",
            TestPaymentTransactionId,
            400000m,
            "VND",
            RefundReason.CustomerRequest,
            "Previous refund",
            "other-user",
            TestTenantId);
        existingRefund.Approve("approver");

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paidPayment);

        _refundRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<RefundsByPaymentSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Refund> { existingRefund });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.RefundAmountExceedsBalance);
    }

    [Fact]
    public async Task Handle_WithEmptyUserId_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand(userId: "");
        var paidPayment = CreateTestPayment(PaymentStatus.Paid, paidAt: DateTimeOffset.UtcNow.AddDays(-5));

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paidPayment);

        _refundRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<RefundsByPaymentSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Refund>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.InvalidRequesterId);
    }

    #endregion
}
