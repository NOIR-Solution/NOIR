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
        result.IsSuccess.ShouldBe(true);
        result.Value.TransactionNumber.ShouldBe("TXN-001");
        result.Value.Provider.ShouldBe("vnpay");
        result.Value.Amount.ShouldBe(100000m);
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
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value;
        dto.Id.ShouldBe(transaction.Id);
        dto.TransactionNumber.ShouldBe("TXN-002");
        dto.Provider.ShouldBe("momo");
        dto.OrderId.ShouldBe(orderId);
        dto.CustomerId.ShouldBe(customerId);
        dto.Amount.ShouldBe(250000m);
        dto.Currency.ShouldBe("VND");
        dto.GatewayFee.ShouldBe(2500m);
        dto.NetAmount.ShouldBe(247500m);
        dto.Status.ShouldBe(PaymentStatus.Pending);
        dto.PaymentMethod.ShouldBe(PaymentMethod.EWallet);
        dto.ExpiresAt.ShouldNotBeNull();
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
        result.IsSuccess.ShouldBe(true);
        result.Value.Status.ShouldBe(PaymentStatus.Paid);
        result.Value.GatewayTransactionId.ShouldBe("gateway-txn-123");
        result.Value.PaidAt.ShouldNotBeNull();
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
        result.IsSuccess.ShouldBe(true);
        result.Value.Status.ShouldBe(PaymentStatus.Failed);
        result.Value.FailureReason.ShouldBe("Insufficient funds");
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
        result.IsSuccess.ShouldBe(true);
        result.Value.PaymentMethod.ShouldBe(PaymentMethod.COD);
        result.Value.CodCollectorName.ShouldBe("Delivery Agent");
        result.Value.CodCollectedAt.ShouldNotBeNull();
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
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe(ErrorCodes.Payment.TransactionNotFound);
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
