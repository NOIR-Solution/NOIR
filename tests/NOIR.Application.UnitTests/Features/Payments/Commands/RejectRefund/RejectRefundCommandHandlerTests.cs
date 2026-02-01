namespace NOIR.Application.UnitTests.Features.Payments.Commands.RejectRefund;

using NOIR.Application.Features.Payments.Commands.RejectRefund;
using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Specifications;

/// <summary>
/// Unit tests for RejectRefundCommandHandler.
/// Tests refund rejection scenarios with mocked dependencies.
/// </summary>
public class RejectRefundCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Refund, Guid>> _refundRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPaymentHubContext> _paymentHubContextMock;
    private readonly RejectRefundCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestRefundNumber = "REF-20260131-001";
    private const string TestUserId = "user-123";
    private static readonly Guid TestRefundId = Guid.NewGuid();
    private static readonly Guid TestPaymentTransactionId = Guid.NewGuid();

    public RejectRefundCommandHandlerTests()
    {
        _refundRepositoryMock = new Mock<IRepository<Refund, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _paymentHubContextMock = new Mock<IPaymentHubContext>();

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

        _handler = new RejectRefundCommandHandler(
            _refundRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _paymentHubContextMock.Object);
    }

    private static RejectRefundCommand CreateTestCommand(
        Guid? refundId = null,
        string rejectionReason = "Refund policy violation",
        string? userId = TestUserId)
    {
        return new RejectRefundCommand(
            refundId ?? TestRefundId,
            rejectionReason) with { UserId = userId };
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
            refund.Reject("Previous rejection reason");
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
    public async Task Handle_WithPendingRefund_ShouldRejectSuccessfully()
    {
        // Arrange
        var command = CreateTestCommand(rejectionReason: "Not eligible for refund");
        var pendingRefund = CreateTestRefund(RefundStatus.Pending);

        _refundRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<RefundByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingRefund);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Status.Should().Be(RefundStatus.Rejected);
        result.Value.RefundNumber.Should().Be(TestRefundNumber);
        result.Value.ReasonDetail.Should().Be("Not eligible for refund");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _paymentHubContextMock.Verify(x => x.SendRefundStatusUpdateAsync(
            TestRefundId,
            TestRefundNumber,
            TestPaymentTransactionId,
            "Rejected",
            It.IsAny<decimal>(),
            "Not eligible for refund",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDefaultRejectionReason_ShouldUseRejected()
    {
        // Arrange
        var command = new RejectRefundCommand(TestRefundId, null!) with { UserId = TestUserId };
        var pendingRefund = CreateTestRefund(RefundStatus.Pending);

        _refundRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<RefundByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingRefund);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ReasonDetail.Should().Be("Rejected");
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
