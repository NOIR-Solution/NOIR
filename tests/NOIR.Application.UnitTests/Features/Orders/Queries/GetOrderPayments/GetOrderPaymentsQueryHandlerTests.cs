using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Queries.GetOrderPayments;
using NOIR.Application.Features.Payments.Specifications;

namespace NOIR.Application.UnitTests.Features.Orders.Queries.GetOrderPayments;

/// <summary>
/// Unit tests for GetOrderPaymentsQueryHandler.
/// Tests payment transaction retrieval for orders with mocked dependencies.
/// </summary>
public class GetOrderPaymentsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentTransaction, Guid>> _paymentRepositoryMock;
    private readonly GetOrderPaymentsQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetOrderPaymentsQueryHandlerTests()
    {
        _paymentRepositoryMock = new Mock<IRepository<PaymentTransaction, Guid>>();
        _handler = new GetOrderPaymentsQueryHandler(_paymentRepositoryMock.Object);
    }

    private static PaymentTransaction CreateTestPaymentTransaction(
        string transactionNumber = "TXN-20250126-0001",
        Guid? paymentGatewayId = null,
        string provider = "VNPay",
        decimal amount = 100.00m,
        string currency = "VND",
        PaymentMethod paymentMethod = PaymentMethod.BankTransfer,
        PaymentStatus status = PaymentStatus.Pending,
        Guid? orderId = null)
    {
        var transaction = PaymentTransaction.Create(
            transactionNumber,
            paymentGatewayId ?? Guid.NewGuid(),
            provider,
            amount,
            currency,
            paymentMethod,
            Guid.NewGuid().ToString(), // idempotency key
            TestTenantId);

        if (orderId.HasValue)
        {
            transaction.SetOrderId(orderId.Value);
        }

        if (status == PaymentStatus.Paid)
        {
            transaction.MarkAsPaid("GTX-12345");
        }
        else if (status == PaymentStatus.Failed)
        {
            transaction.MarkAsFailed("Payment declined", "DECLINED");
        }
        else if (status == PaymentStatus.Cancelled)
        {
            transaction.MarkAsCancelled();
        }

        return transaction;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidOrderId_ShouldReturnPayments()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var payments = new List<PaymentTransaction>
        {
            CreateTestPaymentTransaction(transactionNumber: "TXN-001", orderId: orderId),
            CreateTestPaymentTransaction(transactionNumber: "TXN-002", orderId: orderId)
        };

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var gatewayId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var transaction = CreateTestPaymentTransaction(
            transactionNumber: "TXN-20250126-0042",
            paymentGatewayId: gatewayId,
            provider: "VNPay",
            amount: 250.00m,
            currency: "VND",
            paymentMethod: PaymentMethod.CreditCard,
            status: PaymentStatus.Paid,
            orderId: orderId);

        transaction.SetCustomerId(customerId);
        transaction.SetGatewayFee(5.00m);

        var payments = new List<PaymentTransaction> { transaction };

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        var dto = result.Value[0];
        dto.TransactionNumber.Should().Be("TXN-20250126-0042");
        dto.PaymentGatewayId.Should().Be(gatewayId);
        dto.Provider.Should().Be("VNPay");
        dto.OrderId.Should().Be(orderId);
        dto.CustomerId.Should().Be(customerId);
        dto.Amount.Should().Be(250.00m);
        dto.Currency.Should().Be("VND");
        dto.PaymentMethod.Should().Be(PaymentMethod.CreditCard);
        dto.Status.Should().Be(PaymentStatus.Paid);
        dto.GatewayFee.Should().Be(5.00m);
        dto.NetAmount.Should().Be(245.00m);
        dto.GatewayTransactionId.Should().Be("GTX-12345");
        dto.PaidAt.Should().NotBeNull();
    }

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
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithMultiplePaymentMethods_ShouldReturnAllPaymentMethods()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var payments = new List<PaymentTransaction>
        {
            CreateTestPaymentTransaction(transactionNumber: "TXN-001", paymentMethod: PaymentMethod.BankTransfer, orderId: orderId),
            CreateTestPaymentTransaction(transactionNumber: "TXN-002", paymentMethod: PaymentMethod.CreditCard, orderId: orderId),
            CreateTestPaymentTransaction(transactionNumber: "TXN-003", paymentMethod: PaymentMethod.COD, orderId: orderId),
            CreateTestPaymentTransaction(transactionNumber: "TXN-004", paymentMethod: PaymentMethod.EWallet, orderId: orderId)
        };

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(4);
        result.Value.Should().Contain(p => p.PaymentMethod == PaymentMethod.BankTransfer);
        result.Value.Should().Contain(p => p.PaymentMethod == PaymentMethod.CreditCard);
        result.Value.Should().Contain(p => p.PaymentMethod == PaymentMethod.COD);
        result.Value.Should().Contain(p => p.PaymentMethod == PaymentMethod.EWallet);
    }

    [Fact]
    public async Task Handle_WithMultiplePaymentStatuses_ShouldReturnAllStatuses()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var pendingPayment = CreateTestPaymentTransaction(transactionNumber: "TXN-001", status: PaymentStatus.Pending, orderId: orderId);
        var paidPayment = CreateTestPaymentTransaction(transactionNumber: "TXN-002", status: PaymentStatus.Paid, orderId: orderId);
        var failedPayment = CreateTestPaymentTransaction(transactionNumber: "TXN-003", status: PaymentStatus.Failed, orderId: orderId);

        var payments = new List<PaymentTransaction> { pendingPayment, paidPayment, failedPayment };

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().Contain(p => p.Status == PaymentStatus.Pending);
        result.Value.Should().Contain(p => p.Status == PaymentStatus.Paid);
        result.Value.Should().Contain(p => p.Status == PaymentStatus.Failed);
    }

    [Fact]
    public async Task Handle_WithDifferentProviders_ShouldReturnAllProviders()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var payments = new List<PaymentTransaction>
        {
            CreateTestPaymentTransaction(transactionNumber: "TXN-001", provider: "VNPay", orderId: orderId),
            CreateTestPaymentTransaction(transactionNumber: "TXN-002", provider: "MoMo", orderId: orderId),
            CreateTestPaymentTransaction(transactionNumber: "TXN-003", provider: "ZaloPay", orderId: orderId)
        };

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().Contain(p => p.Provider == "VNPay");
        result.Value.Should().Contain(p => p.Provider == "MoMo");
        result.Value.Should().Contain(p => p.Provider == "ZaloPay");
    }

    #endregion

    #region Payment Status Scenarios

    [Fact]
    public async Task Handle_WithPaidPayment_ShouldReturnPaymentWithPaidAt()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var paidPayment = CreateTestPaymentTransaction(
            transactionNumber: "TXN-001",
            status: PaymentStatus.Paid,
            orderId: orderId);

        var payments = new List<PaymentTransaction> { paidPayment };

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Status.Should().Be(PaymentStatus.Paid);
        result.Value[0].PaidAt.Should().NotBeNull();
        result.Value[0].GatewayTransactionId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithFailedPayment_ShouldReturnPaymentWithFailureReason()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var failedPayment = CreateTestPaymentTransaction(
            transactionNumber: "TXN-001",
            status: PaymentStatus.Failed,
            orderId: orderId);

        var payments = new List<PaymentTransaction> { failedPayment };

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Status.Should().Be(PaymentStatus.Failed);
        result.Value[0].FailureReason.Should().Be("Payment declined");
    }

    [Fact]
    public async Task Handle_WithCancelledPayment_ShouldReturnCancelledStatus()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var cancelledPayment = CreateTestPaymentTransaction(
            transactionNumber: "TXN-001",
            status: PaymentStatus.Cancelled,
            orderId: orderId);

        var payments = new List<PaymentTransaction> { cancelledPayment };

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Status.Should().Be(PaymentStatus.Cancelled);
    }

    #endregion

    #region COD Payment Scenarios

    [Fact]
    public async Task Handle_WithCodPayment_ShouldReturnCodDetails()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var codPayment = CreateTestPaymentTransaction(
            transactionNumber: "TXN-001",
            paymentMethod: PaymentMethod.COD,
            status: PaymentStatus.Pending,
            orderId: orderId);

        var payments = new List<PaymentTransaction> { codPayment };

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].PaymentMethod.Should().Be(PaymentMethod.COD);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentTransaction>());

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _paymentRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PaymentTransactionsByOrderSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaymentsWithTimestamps()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var payment = CreateTestPaymentTransaction(
            transactionNumber: "TXN-001",
            orderId: orderId);

        var payments = new List<PaymentTransaction> { payment };

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task Handle_WithDifferentCurrencies_ShouldReturnCorrectCurrencies()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var payments = new List<PaymentTransaction>
        {
            CreateTestPaymentTransaction(transactionNumber: "TXN-001", currency: "VND", amount: 100000m, orderId: orderId),
            CreateTestPaymentTransaction(transactionNumber: "TXN-002", currency: "USD", amount: 50m, orderId: orderId)
        };

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(p => p.Currency == "VND" && p.Amount == 100000m);
        result.Value.Should().Contain(p => p.Currency == "USD" && p.Amount == 50m);
    }

    [Fact]
    public async Task Handle_WithPaymentExpiration_ShouldReturnExpiresAt()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        var payment = CreateTestPaymentTransaction(
            transactionNumber: "TXN-001",
            orderId: orderId);
        payment.SetExpiresAt(expiresAt);

        var payments = new List<PaymentTransaction> { payment };

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsByOrderSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        var query = new GetOrderPaymentsQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public async Task Handle_ShouldUseCorrectSpecification()
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
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _paymentRepositoryMock.Verify(
            x => x.ListAsync(
                It.Is<PaymentTransactionsByOrderSpec>(spec => spec != null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
