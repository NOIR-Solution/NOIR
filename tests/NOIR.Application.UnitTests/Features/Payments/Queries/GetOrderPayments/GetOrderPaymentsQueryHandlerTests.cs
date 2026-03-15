using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Queries.GetOrderPayments;
using NOIR.Application.Features.Payments.Specifications;

namespace NOIR.Application.UnitTests.Features.Payments.Queries.GetOrderPayments;

/// <summary>
/// Unit tests for GetOrderPaymentsQueryHandler.
/// Tests retrieval of payment transactions for a specific order.
/// </summary>
public class GetOrderPaymentsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentTransaction, Guid>> _paymentRepositoryMock;
    private readonly GetOrderPaymentsQueryHandler _handler;

    public GetOrderPaymentsQueryHandlerTests()
    {
        _paymentRepositoryMock = new Mock<IRepository<PaymentTransaction, Guid>>();
        _handler = new GetOrderPaymentsQueryHandler(_paymentRepositoryMock.Object);
    }

    private static PaymentTransaction CreateTestTransaction(
        Guid orderId,
        string transactionNumber = "TXN-001",
        string provider = "vnpay",
        decimal amount = 100000m,
        PaymentStatus status = PaymentStatus.Pending)
    {
        var gatewayId = Guid.NewGuid();
        var transaction = PaymentTransaction.Create(
            transactionNumber,
            gatewayId,
            provider,
            amount,
            "VND",
            PaymentMethod.EWallet,
            Guid.NewGuid().ToString(),
            "tenant-123");
        transaction.SetOrderId(orderId);
        return transaction;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithMultiplePayments_ShouldReturnAllPaymentsForOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var transactions = new List<PaymentTransaction>
        {
            CreateTestTransaction(orderId, "TXN-001", "vnpay", 100000m, PaymentStatus.Failed),
            CreateTestTransaction(orderId, "TXN-002", "momo", 100000m, PaymentStatus.Paid)
        };

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Count().ShouldBe(2);
        result.Value[0].TransactionNumber.ShouldBe("TXN-001");
        result.Value[1].TransactionNumber.ShouldBe("TXN-002");
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var transaction = CreateTestTransaction(orderId, "TXN-001", "momo", 250000m, PaymentStatus.Paid);
        transaction.SetCustomerId(customerId);
        transaction.MarkAsPaid("gateway-txn-123");
        transaction.SetGatewayFee(2500m);

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentTransaction> { transaction });

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value[0];
        dto.Id.ShouldBe(transaction.Id);
        dto.TransactionNumber.ShouldBe("TXN-001");
        dto.GatewayTransactionId.ShouldBe("gateway-txn-123");
        dto.Provider.ShouldBe("momo");
        dto.OrderId.ShouldBe(orderId);
        dto.CustomerId.ShouldBe(customerId);
        dto.Amount.ShouldBe(250000m);
        dto.Currency.ShouldBe("VND");
        dto.GatewayFee.ShouldBe(2500m);
        dto.NetAmount.ShouldBe(247500m);
        dto.Status.ShouldBe(PaymentStatus.Paid);
        dto.PaymentMethod.ShouldBe(PaymentMethod.EWallet);
        dto.PaidAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_WithSinglePayment_ShouldReturnSingleDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var transaction = CreateTestTransaction(orderId);

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentTransaction> { transaction });

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Count().ShouldBe(1);
    }

    [Fact]
    public async Task Handle_WithCodPayment_ShouldIncludeCodInfo()
    {
        // Arrange
        var orderId = Guid.NewGuid();
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
        transaction.SetOrderId(orderId);
        transaction.ConfirmCodCollection("Delivery Driver");

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentTransaction> { transaction });

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value[0];
        dto.PaymentMethod.ShouldBe(PaymentMethod.COD);
        dto.CodCollectorName.ShouldBe("Delivery Driver");
        dto.CodCollectedAt.ShouldNotBeNull();
    }

    #endregion

    #region Empty Results Scenarios

    [Fact]
    public async Task Handle_WithNoPayments_ShouldReturnEmptyList()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentTransaction>());

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBeEmpty();
    }

    #endregion

    #region CancellationToken Propagation

    [Fact]
    public async Task Handle_ShouldPropagateCancellationToken()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                token))
            .ReturnsAsync(new List<PaymentTransaction>());

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _paymentRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PaymentTransactionsByOrderSpec>(), token),
            Times.Once);
    }

    #endregion
}
