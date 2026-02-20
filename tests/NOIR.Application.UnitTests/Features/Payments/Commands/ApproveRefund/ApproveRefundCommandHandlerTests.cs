namespace NOIR.Application.UnitTests.Features.Payments.Commands.ApproveRefund;

using NOIR.Application.Features.Payments.Commands.ApproveRefund;
using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Specifications;

/// <summary>
/// Unit tests for ApproveRefundCommandHandler.
/// Tests refund approval scenarios with mocked dependencies.
/// </summary>
public class ApproveRefundCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Refund, Guid>> _refundRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPaymentHubContext> _paymentHubContextMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly ApproveRefundCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestRefundNumber = "REF-20260131-001";
    private const string TestUserId = "user-123";
    private static readonly Guid TestRefundId = Guid.NewGuid();
    private static readonly Guid TestPaymentTransactionId = Guid.NewGuid();

    public ApproveRefundCommandHandlerTests()
    {
        _refundRepositoryMock = new Mock<IRepository<Refund, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _paymentHubContextMock = new Mock<IPaymentHubContext>();
        _paymentServiceMock = new Mock<IPaymentService>();

        // Default setup
        _paymentHubContextMock
            .Setup(x => x.SendRefundStatusUpdateAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _paymentServiceMock
            .Setup(x => x.ProcessRefundAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefundResult(true, null, null));

        _handler = new ApproveRefundCommandHandler(
            _refundRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _paymentHubContextMock.Object,
            _paymentServiceMock.Object);
    }

    private static ApproveRefundCommand CreateTestCommand(
        Guid? refundId = null,
        string? notes = "Approved by admin",
        string? userId = TestUserId)
    {
        return new ApproveRefundCommand(
            refundId ?? TestRefundId,
            notes) with { UserId = userId };
    }

    private static Refund CreateTestRefund(
        RefundStatus status = RefundStatus.Pending,
        decimal amount = 100000m)
    {
        var refund = Refund.Create(
            TestRefundNumber,
            TestPaymentTransactionId,
            amount,
            "VND",
            RefundReason.CustomerRequest,
            "Customer wants refund",
            "requester-user-id",
            TestTenantId);

        typeof(Refund).GetProperty("Id")?.SetValue(refund, TestRefundId);

        // Set status if not pending (default)
        if (status == RefundStatus.Approved)
        {
            refund.Approve("approver-user-id");
        }
        else if (status == RefundStatus.Rejected)
        {
            refund.Reject("Rejected reason");
        }
        else if (status == RefundStatus.Completed)
        {
            refund.Approve("approver-user-id");
            refund.Complete("GW-REFUND-001");
        }

        return refund;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithPendingRefund_ShouldApproveSuccessfully()
    {
        // Arrange
        var command = CreateTestCommand();
        var pendingRefund = CreateTestRefund(RefundStatus.Pending);

        _refundRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<RefundByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingRefund);

        // Re-fetch after processing returns the same refund (now approved)
        _refundRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<RefundByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingRefund);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Status.Should().Be(RefundStatus.Approved);
        result.Value.RefundNumber.Should().Be(TestRefundNumber);
        result.Value.ApprovedBy.Should().Be(TestUserId);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _paymentHubContextMock.Verify(x => x.SendRefundStatusUpdateAsync(
            TestRefundId,
            TestRefundNumber,
            TestPaymentTransactionId,
            "Approved",
            It.IsAny<decimal>(),
            "Refund approved",
            It.IsAny<CancellationToken>()), Times.Once);
        _paymentServiceMock.Verify(x => x.ProcessRefundAsync(TestRefundId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDifferentAmounts_ShouldApproveCorrectly()
    {
        // Arrange
        var command = CreateTestCommand();
        var refund = CreateTestRefund(RefundStatus.Pending, amount: 250000m);

        _refundRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<RefundByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refund);

        // Re-fetch after processing returns the same refund
        _refundRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<RefundByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refund);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(250000m);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenRefundNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = CreateTestCommand();

        _refundRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<RefundByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Refund?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be(ErrorCodes.Payment.RefundNotFound);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _paymentHubContextMock.Verify(x => x.SendRefundStatusUpdateAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<decimal>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRefundAlreadyApproved_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand();
        var approvedRefund = CreateTestRefund(RefundStatus.Approved);

        _refundRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<RefundByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approvedRefund);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.InvalidRefundStatus);
        result.Error.Message.Should().Contain("pending");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRefundAlreadyRejected_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand();
        var rejectedRefund = CreateTestRefund(RefundStatus.Rejected);

        _refundRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<RefundByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rejectedRefund);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.InvalidRefundStatus);
    }

    [Fact]
    public async Task Handle_WithEmptyUserId_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand(userId: "");
        var pendingRefund = CreateTestRefund(RefundStatus.Pending);

        _refundRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<RefundByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingRefund);

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
        var pendingRefund = CreateTestRefund(RefundStatus.Pending);

        _refundRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<RefundByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingRefund);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.InvalidRequesterId);
    }

    #endregion
}
