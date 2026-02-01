using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Queries.GetRefunds;
using NOIR.Application.Features.Payments.Specifications;

namespace NOIR.Application.UnitTests.Features.Payments.Queries.GetRefunds;

/// <summary>
/// Unit tests for GetRefundsQueryHandler.
/// Tests retrieval of refunds for a specific payment transaction.
/// </summary>
public class GetRefundsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Refund, Guid>> _refundRepositoryMock;
    private readonly GetRefundsQueryHandler _handler;

    public GetRefundsQueryHandlerTests()
    {
        _refundRepositoryMock = new Mock<IRepository<Refund, Guid>>();
        _handler = new GetRefundsQueryHandler(_refundRepositoryMock.Object);
    }

    private static Refund CreateTestRefund(
        Guid paymentTransactionId,
        string refundNumber = "REF-001",
        decimal amount = 50000m,
        RefundReason reason = RefundReason.CustomerRequest,
        RefundStatus status = RefundStatus.Pending)
    {
        var refund = Refund.Create(
            refundNumber,
            paymentTransactionId,
            amount,
            "VND",
            reason,
            "Customer requested refund",
            "admin@example.com",
            "tenant-123");
        return refund;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithMultipleRefunds_ShouldReturnAllRefundsForTransaction()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var refunds = new List<Refund>
        {
            CreateTestRefund(transactionId, "REF-001", 25000m, RefundReason.CustomerRequest),
            CreateTestRefund(transactionId, "REF-002", 25000m, RefundReason.Defective)
        };

        _refundRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<RefundsByPaymentSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(refunds);

        var query = new GetRefundsQuery(transactionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].RefundNumber.Should().Be("REF-001");
        result.Value[1].RefundNumber.Should().Be("REF-002");
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var refund = CreateTestRefund(transactionId, "REF-001", 50000m, RefundReason.CustomerRequest);
        refund.Approve("manager@example.com");
        refund.Complete("gateway-refund-123");

        _refundRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<RefundsByPaymentSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Refund> { refund });

        var query = new GetRefundsQuery(transactionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value[0];
        dto.Id.Should().Be(refund.Id);
        dto.RefundNumber.Should().Be("REF-001");
        dto.PaymentTransactionId.Should().Be(transactionId);
        dto.GatewayRefundId.Should().Be("gateway-refund-123");
        dto.Amount.Should().Be(50000m);
        dto.Currency.Should().Be("VND");
        dto.Status.Should().Be(RefundStatus.Completed);
        dto.Reason.Should().Be(RefundReason.CustomerRequest);
        dto.ReasonDetail.Should().Be("Customer requested refund");
        dto.RequestedBy.Should().Be("admin@example.com");
        dto.ApprovedBy.Should().Be("manager@example.com");
        dto.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithSingleRefund_ShouldReturnSingleDto()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var refund = CreateTestRefund(transactionId);

        _refundRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<RefundsByPaymentSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Refund> { refund });

        var query = new GetRefundsQuery(transactionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithPendingRefund_ShouldReturnPendingStatus()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var refund = CreateTestRefund(transactionId, status: RefundStatus.Pending);

        _refundRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<RefundsByPaymentSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Refund> { refund });

        var query = new GetRefundsQuery(transactionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[0].Status.Should().Be(RefundStatus.Pending);
        result.Value[0].ApprovedBy.Should().BeNull();
        result.Value[0].ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithDifferentReasons_ShouldMapCorrectly()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var refunds = new List<Refund>
        {
            CreateTestRefund(transactionId, "REF-001", reason: RefundReason.CustomerRequest),
            CreateTestRefund(transactionId, "REF-002", reason: RefundReason.Defective),
            CreateTestRefund(transactionId, "REF-003", reason: RefundReason.WrongItem)
        };

        _refundRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<RefundsByPaymentSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(refunds);

        var query = new GetRefundsQuery(transactionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(r => r.Reason == RefundReason.CustomerRequest);
        result.Value.Should().Contain(r => r.Reason == RefundReason.Defective);
        result.Value.Should().Contain(r => r.Reason == RefundReason.WrongItem);
    }

    #endregion

    #region Empty Results Scenarios

    [Fact]
    public async Task Handle_WithNoRefunds_ShouldReturnEmptyList()
    {
        // Arrange
        var transactionId = Guid.NewGuid();

        _refundRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<RefundsByPaymentSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Refund>());

        var query = new GetRefundsQuery(transactionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region CancellationToken Propagation

    [Fact]
    public async Task Handle_ShouldPropagateCancellationToken()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _refundRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<RefundsByPaymentSpec>(),
                token))
            .ReturnsAsync(new List<Refund>());

        var query = new GetRefundsQuery(transactionId);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _refundRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<RefundsByPaymentSpec>(), token),
            Times.Once);
    }

    #endregion
}
