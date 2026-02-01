using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Queries.GetPaymentTransactions;
using NOIR.Application.Features.Payments.Specifications;

namespace NOIR.Application.UnitTests.Features.Payments.Queries.GetPaymentTransactions;

/// <summary>
/// Unit tests for GetPaymentTransactionsQueryHandler.
/// Tests paginated retrieval of payment transactions with filtering.
/// </summary>
public class GetPaymentTransactionsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentTransaction, Guid>> _paymentRepositoryMock;
    private readonly GetPaymentTransactionsQueryHandler _handler;

    public GetPaymentTransactionsQueryHandlerTests()
    {
        _paymentRepositoryMock = new Mock<IRepository<PaymentTransaction, Guid>>();
        _handler = new GetPaymentTransactionsQueryHandler(_paymentRepositoryMock.Object);
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

    private static List<PaymentTransaction> CreateTestTransactions(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestTransaction($"TXN-{i:D3}", "vnpay", 100000m * i))
            .ToList();
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDefaultPaging_ShouldReturnPagedResult()
    {
        // Arrange
        var transactions = CreateTestTransactions(5);

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        _paymentRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentTransactionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var query = new GetPaymentTransactionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(5);
        result.Value.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithPaging_ShouldReturnCorrectPage()
    {
        // Arrange
        var page2Transactions = CreateTestTransactions(10);

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page2Transactions);

        _paymentRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentTransactionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        var query = new GetPaymentTransactionsQuery(Page: 2, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
        result.Value.TotalCount.Should().Be(25);
        result.Value.PageNumber.Should().Be(2);
        result.Value.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldMapToListDto()
    {
        // Arrange
        var transaction = CreateTestTransaction("TXN-001", "momo", 250000m, PaymentStatus.Paid, PaymentMethod.EWallet);
        transaction.MarkAsPaid("gateway-123");

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentTransaction> { transaction });

        _paymentRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentTransactionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetPaymentTransactionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Items[0];
        dto.Id.Should().Be(transaction.Id);
        dto.TransactionNumber.Should().Be("TXN-001");
        dto.Provider.Should().Be("momo");
        dto.Amount.Should().Be(250000m);
        dto.Currency.Should().Be("VND");
        dto.Status.Should().Be(PaymentStatus.Paid);
        dto.PaymentMethod.Should().Be(PaymentMethod.EWallet);
        dto.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldPassToSpecification()
    {
        // Arrange
        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentTransaction>());

        _paymentRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentTransactionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetPaymentTransactionsQuery(Status: PaymentStatus.Paid);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _paymentRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PaymentTransactionsSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithPaymentMethodFilter_ShouldPassToSpecification()
    {
        // Arrange
        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentTransaction>());

        _paymentRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentTransactionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetPaymentTransactionsQuery(PaymentMethod: PaymentMethod.COD);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _paymentRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PaymentTransactionsSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithProviderFilter_ShouldPassToSpecification()
    {
        // Arrange
        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentTransaction>());

        _paymentRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentTransactionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetPaymentTransactionsQuery(Provider: "vnpay");

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _paymentRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PaymentTransactionsSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSearchFilter_ShouldPassToSpecification()
    {
        // Arrange
        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentTransaction>());

        _paymentRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentTransactionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetPaymentTransactionsQuery(Search: "TXN-001");

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _paymentRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PaymentTransactionsSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Empty Results Scenarios

    [Fact]
    public async Task Handle_WithNoTransactions_ShouldReturnEmptyPagedResult()
    {
        // Arrange
        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentTransaction>());

        _paymentRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentTransactionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetPaymentTransactionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
    }

    #endregion

    #region CancellationToken Propagation

    [Fact]
    public async Task Handle_ShouldPropagateCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentTransactionsSpec>(),
                token))
            .ReturnsAsync(new List<PaymentTransaction>());

        _paymentRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentTransactionsSpec>(),
                token))
            .ReturnsAsync(0);

        var query = new GetPaymentTransactionsQuery();

        // Act
        await _handler.Handle(query, token);

        // Assert
        _paymentRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PaymentTransactionsSpec>(), token),
            Times.Once);
        _paymentRepositoryMock.Verify(
            x => x.CountAsync(It.IsAny<PaymentTransactionsSpec>(), token),
            Times.Once);
    }

    #endregion
}
