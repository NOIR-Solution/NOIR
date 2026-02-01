using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Queries.GetPaymentTransaction;
using NOIR.Application.Features.Payments.Specifications;

namespace NOIR.Application.UnitTests.Features.Payments.Queries.GetPaymentTransaction;

/// <summary>
/// Unit tests for GetPaymentTransactionQueryHandler.
/// Tests retrieval of a single payment transaction by ID.
/// </summary>
public class GetPaymentTransactionQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentTransaction, Guid>> _paymentRepositoryMock;
    private readonly GetPaymentTransactionQueryHandler _handler;

    public GetPaymentTransactionQueryHandlerTests()
    {
        _paymentRepositoryMock = new Mock<IRepository<PaymentTransaction, Guid>>();
        _handler = new GetPaymentTransactionQueryHandler(_paymentRepositoryMock.Object);
    }

    private static PaymentTransaction CreateTestTransaction(
        string transactionNumber = "TXN-001",
        string provider = "vnpay",
        decimal amount = 100000m,
        PaymentStatus status = PaymentStatus.Pending,
        PaymentMethod paymentMethod = PaymentMethod.EWallet)
    {
        var gatewayId = Guid.NewGuid();
        var transaction = PaymentTransaction.Create(
            transactionNumber,
            gatewayId,
            provider,
            amount,
            "VND",
            paymentMethod,
            Guid.NewGuid().ToString(),
            "tenant-123");

        return transaction;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenTransactionExists_ShouldReturnPaymentTransactionDto()
    {
        // Arrange
        var transaction = CreateTestTransaction("TXN-001", "vnpay", 100000m);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PaymentTransactionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var query = new GetPaymentTransactionQuery(transaction.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TransactionNumber.Should().Be("TXN-001");
        result.Value.Provider.Should().Be("vnpay");
        result.Value.Amount.Should().Be(100000m);
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var transaction = CreateTestTransaction("TXN-002", "momo", 250000m, PaymentStatus.Pending, PaymentMethod.EWallet);
        transaction.SetOrderId(orderId);
        transaction.SetCustomerId(customerId);
        transaction.SetGatewayFee(2500m);
        transaction.SetExpiresAt(DateTimeOffset.UtcNow.AddMinutes(15));

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PaymentTransactionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var query = new GetPaymentTransactionQuery(transaction.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Id.Should().Be(transaction.Id);
        dto.TransactionNumber.Should().Be("TXN-002");
        dto.Provider.Should().Be("momo");
        dto.OrderId.Should().Be(orderId);
        dto.CustomerId.Should().Be(customerId);
        dto.Amount.Should().Be(250000m);
        dto.Currency.Should().Be("VND");
        dto.GatewayFee.Should().Be(2500m);
        dto.NetAmount.Should().Be(247500m);
        dto.Status.Should().Be(PaymentStatus.Pending);
        dto.PaymentMethod.Should().Be(PaymentMethod.EWallet);
        dto.ExpiresAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithPaidTransaction_ShouldIncludePaidAt()
    {
        // Arrange
        var transaction = CreateTestTransaction("TXN-003", "zalopay", 500000m);
        transaction.MarkAsPaid("gateway-txn-123");

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PaymentTransactionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var query = new GetPaymentTransactionQuery(transaction.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentStatus.Paid);
        result.Value.GatewayTransactionId.Should().Be("gateway-txn-123");
        result.Value.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithFailedTransaction_ShouldIncludeFailureReason()
    {
        // Arrange
        var transaction = CreateTestTransaction("TXN-004", "vnpay", 100000m);
        transaction.MarkAsFailed("Insufficient funds", "ERR_INSUFFICIENT");

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PaymentTransactionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var query = new GetPaymentTransactionQuery(transaction.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentStatus.Failed);
        result.Value.FailureReason.Should().Be("Insufficient funds");
    }

    [Fact]
    public async Task Handle_WithCodTransaction_ShouldIncludeCodInfo()
    {
        // Arrange
        var gatewayId = Guid.NewGuid();
        var transaction = PaymentTransaction.Create(
            "TXN-COD-001",
            gatewayId,
            "cod",
            150000m,
            "VND",
            PaymentMethod.COD,
            Guid.NewGuid().ToString(),
            "tenant-123");
        transaction.ConfirmCodCollection("Delivery Agent");

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PaymentTransactionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var query = new GetPaymentTransactionQuery(transaction.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentMethod.Should().Be(PaymentMethod.COD);
        result.Value.CodCollectorName.Should().Be("Delivery Agent");
        result.Value.CodCollectedAt.Should().NotBeNull();
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenTransactionNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var transactionId = Guid.NewGuid();

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PaymentTransactionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        var query = new GetPaymentTransactionQuery(transactionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Payment.TransactionNotFound);
    }

    #endregion

    #region CancellationToken Propagation

    [Fact]
    public async Task Handle_ShouldPropagateCancellationToken()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transaction = CreateTestTransaction();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PaymentTransactionByIdSpec>(),
                token))
            .ReturnsAsync(transaction);

        var query = new GetPaymentTransactionQuery(transactionId);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _paymentRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdSpec>(), token),
            Times.Once);
    }

    #endregion
}
