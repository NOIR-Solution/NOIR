using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Queries.GetPendingCodPayments;
using NOIR.Application.Features.Payments.Specifications;

namespace NOIR.Application.UnitTests.Features.Payments.Queries.GetPendingCodPayments;

/// <summary>
/// Unit tests for GetPendingCodPaymentsQueryHandler.
/// Tests paginated retrieval of pending COD payments awaiting collection.
/// </summary>
public class GetPendingCodPaymentsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentTransaction, Guid>> _paymentRepositoryMock;
    private readonly GetPendingCodPaymentsQueryHandler _handler;

    public GetPendingCodPaymentsQueryHandlerTests()
    {
        _paymentRepositoryMock = new Mock<IRepository<PaymentTransaction, Guid>>();
        _handler = new GetPendingCodPaymentsQueryHandler(_paymentRepositoryMock.Object);
    }

    private static PaymentTransaction CreateTestCodPayment(
        string transactionNumber = "TXN-COD-001",
        decimal amount = 150000m)
    {
        var gatewayId = Guid.NewGuid();
        var transaction = PaymentTransaction.Create(
            transactionNumber,
            gatewayId,
            "cod",
            amount,
            "VND",
            PaymentMethod.COD,
            Guid.NewGuid().ToString(),
            "tenant-123");
        transaction.MarkAsCodPending();
        return transaction;
    }

    private static List<PaymentTransaction> CreateTestCodPayments(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestCodPayment($"TXN-COD-{i:D3}", 100000m * i))
            .ToList();
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDefaultPaging_ShouldReturnPagedResult()
    {
        // Arrange
        var payments = CreateTestCodPayments(5);

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PendingCodPaymentsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        _paymentRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PendingCodPaymentsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var query = new GetPendingCodPaymentsQuery();

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
        var page2Payments = CreateTestCodPayments(10);

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PendingCodPaymentsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page2Payments);

        _paymentRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PendingCodPaymentsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        var query = new GetPendingCodPaymentsQuery(Page: 2, PageSize: 10);

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
        var payment = CreateTestCodPayment("TXN-COD-001", 250000m);

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PendingCodPaymentsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentTransaction> { payment });

        _paymentRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PendingCodPaymentsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetPendingCodPaymentsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Items[0];
        dto.Id.Should().Be(payment.Id);
        dto.TransactionNumber.Should().Be("TXN-COD-001");
        dto.Provider.Should().Be("cod");
        dto.Amount.Should().Be(250000m);
        dto.Currency.Should().Be("VND");
        dto.Status.Should().Be(PaymentStatus.CodPending);
        dto.PaymentMethod.Should().Be(PaymentMethod.COD);
    }

    [Fact]
    public async Task Handle_WithCustomPageSize_ShouldApplyPageSize()
    {
        // Arrange
        var payments = CreateTestCodPayments(5);

        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PendingCodPaymentsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        _paymentRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PendingCodPaymentsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        var query = new GetPendingCodPaymentsQuery(Page: 1, PageSize: 5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.PageSize.Should().Be(5);
        result.Value.TotalPages.Should().Be(10);
    }

    #endregion

    #region Empty Results Scenarios

    [Fact]
    public async Task Handle_WithNoPendingCodPayments_ShouldReturnEmptyPagedResult()
    {
        // Arrange
        _paymentRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PendingCodPaymentsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentTransaction>());

        _paymentRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PendingCodPaymentsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetPendingCodPaymentsQuery();

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
                It.IsAny<PendingCodPaymentsSpec>(),
                token))
            .ReturnsAsync(new List<PaymentTransaction>());

        _paymentRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PendingCodPaymentsSpec>(),
                token))
            .ReturnsAsync(0);

        var query = new GetPendingCodPaymentsQuery();

        // Act
        await _handler.Handle(query, token);

        // Assert
        _paymentRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PendingCodPaymentsSpec>(), token),
            Times.Once);
        _paymentRepositoryMock.Verify(
            x => x.CountAsync(It.IsAny<PendingCodPaymentsSpec>(), token),
            Times.Once);
    }

    #endregion
}
