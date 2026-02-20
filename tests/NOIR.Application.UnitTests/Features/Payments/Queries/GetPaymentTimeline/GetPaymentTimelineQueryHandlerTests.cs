namespace NOIR.Application.UnitTests.Features.Payments.Queries.GetPaymentTimeline;

using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Queries.GetPaymentTimeline;
using NOIR.Application.Features.Payments.Specifications;

/// <summary>
/// Unit tests for GetPaymentTimelineQueryHandler.
/// </summary>
public class GetPaymentTimelineQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentTransaction, Guid>> _paymentRepositoryMock;
    private readonly Mock<IRepository<PaymentOperationLog, Guid>> _operationLogRepositoryMock;
    private readonly Mock<IRepository<PaymentWebhookLog, Guid>> _webhookLogRepositoryMock;
    private readonly Mock<IRepository<Refund, Guid>> _refundRepositoryMock;
    private readonly GetPaymentTimelineQueryHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestTransactionNumber = "PAY-20260131-001";
    private static readonly Guid TestPaymentId = Guid.NewGuid();
    private static readonly Guid TestGatewayId = Guid.NewGuid();

    public GetPaymentTimelineQueryHandlerTests()
    {
        _paymentRepositoryMock = new Mock<IRepository<PaymentTransaction, Guid>>();
        _operationLogRepositoryMock = new Mock<IRepository<PaymentOperationLog, Guid>>();
        _webhookLogRepositoryMock = new Mock<IRepository<PaymentWebhookLog, Guid>>();
        _refundRepositoryMock = new Mock<IRepository<Refund, Guid>>();

        _handler = new GetPaymentTimelineQueryHandler(
            _paymentRepositoryMock.Object,
            _operationLogRepositoryMock.Object,
            _webhookLogRepositoryMock.Object,
            _refundRepositoryMock.Object);
    }

    private static PaymentTransaction CreateTestPayment(bool withPaidAt = false)
    {
        var payment = PaymentTransaction.Create(
            TestTransactionNumber,
            TestGatewayId,
            "vnpay",
            500000m,
            "VND",
            PaymentMethod.CreditCard,
            "idempotency-key",
            TestTenantId);

        typeof(PaymentTransaction).GetProperty("Id")?.SetValue(payment, TestPaymentId);

        if (withPaidAt)
        {
            payment.MarkAsPaid("GW-TXN-001");
        }

        return payment;
    }

    #endregion

    [Fact]
    public async Task Handle_ShouldMergeAndSortAllEvents()
    {
        // Arrange
        var query = new GetPaymentTimelineQuery(TestPaymentId);
        var payment = CreateTestPayment(withPaidAt: true);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _operationLogRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<PaymentOperationLogsByTransactionIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentOperationLog>());

        _webhookLogRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<WebhookLogsByPaymentSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentWebhookLog>());

        _refundRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<RefundsByPaymentSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Refund>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        // Should have creation event + paid event = 2 events
        result.Value.Count.Should().BeGreaterThanOrEqualTo(2);

        // Events should be sorted by timestamp descending
        for (int i = 0; i < result.Value.Count - 1; i++)
        {
            result.Value[i].Timestamp.Should().BeOnOrAfter(result.Value[i + 1].Timestamp);
        }
    }

    [Fact]
    public async Task Handle_ShouldIncludeCreationEvent()
    {
        // Arrange
        var query = new GetPaymentTimelineQuery(TestPaymentId);
        var payment = CreateTestPayment();

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _operationLogRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<PaymentOperationLogsByTransactionIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentOperationLog>());

        _webhookLogRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<WebhookLogsByPaymentSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentWebhookLog>());

        _refundRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<RefundsByPaymentSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Refund>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(e => e.EventType == "StatusChange" && e.Summary.Contains("Payment created"));
    }

    [Fact]
    public async Task Handle_WhenPaymentNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetPaymentTimelineQuery(Guid.NewGuid());

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be(ErrorCodes.Payment.TransactionNotFound);
    }
}
