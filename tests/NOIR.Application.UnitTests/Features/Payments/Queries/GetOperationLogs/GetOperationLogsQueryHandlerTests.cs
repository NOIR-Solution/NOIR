using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Queries.GetOperationLogs;
using NOIR.Application.Features.Payments.Specifications;

namespace NOIR.Application.UnitTests.Features.Payments.Queries.GetOperationLogs;

/// <summary>
/// Unit tests for GetOperationLogsQueryHandler.
/// Tests paginated retrieval of payment operation logs with filtering.
/// </summary>
public class GetOperationLogsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentOperationLog, Guid>> _operationLogRepositoryMock;
    private readonly GetOperationLogsQueryHandler _handler;

    public GetOperationLogsQueryHandlerTests()
    {
        _operationLogRepositoryMock = new Mock<IRepository<PaymentOperationLog, Guid>>();
        _handler = new GetOperationLogsQueryHandler(_operationLogRepositoryMock.Object);
    }

    private static PaymentOperationLog CreateTestOperationLog(
        PaymentOperationType operationType = PaymentOperationType.InitiatePayment,
        string provider = "vnpay",
        bool success = true,
        string correlationId = "corr-123")
    {
        var log = PaymentOperationLog.Create(operationType, provider, correlationId, "tenant-123");
        log.SetTransactionInfo(Guid.NewGuid(), "TXN-001");
        log.SetRequestData("{\"amount\":100000}");
        log.SetResponseData("{\"status\":\"success\"}", 200);
        log.SetDuration(150);
        log.SetUserInfo("user-123", "192.168.1.1");
        if (success)
            log.MarkAsSuccess();
        else
            log.MarkAsFailed("ERROR_CODE", "Something went wrong");
        return log;
    }

    private static List<PaymentOperationLog> CreateTestOperationLogs(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestOperationLog(
                PaymentOperationType.InitiatePayment,
                "vnpay",
                i % 2 == 0,
                $"corr-{i:D3}"))
            .ToList();
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDefaultPaging_ShouldReturnPagedResult()
    {
        // Arrange
        var logs = CreateTestOperationLogs(5);

        _operationLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        _operationLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var query = new GetOperationLogsQuery();

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
        var page2Logs = CreateTestOperationLogs(10);

        _operationLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page2Logs);

        _operationLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        var query = new GetOperationLogsQuery(Page: 2, PageSize: 10);

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
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var log = CreateTestOperationLog(
            PaymentOperationType.InitiatePayment,
            "momo",
            success: true,
            correlationId: "corr-test");

        _operationLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentOperationLog> { log });

        _operationLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetOperationLogsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Items[0];
        dto.Id.Should().Be(log.Id);
        dto.OperationType.Should().Be(PaymentOperationType.InitiatePayment);
        dto.Provider.Should().Be("momo");
        dto.TransactionNumber.Should().Be("TXN-001");
        dto.CorrelationId.Should().Be("corr-test");
        dto.RequestData.Should().Be("{\"amount\":100000}");
        dto.ResponseData.Should().Be("{\"status\":\"success\"}");
        dto.HttpStatusCode.Should().Be(200);
        dto.DurationMs.Should().Be(150);
        dto.Success.Should().BeTrue();
        dto.UserId.Should().Be("user-123");
        dto.IpAddress.Should().Be("192.168.1.1");
    }

    [Fact]
    public async Task Handle_WithProviderFilter_ShouldPassToSpecification()
    {
        // Arrange
        _operationLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentOperationLog>());

        _operationLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetOperationLogsQuery(Provider: "vnpay");

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _operationLogRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PaymentOperationLogsSearchSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithOperationTypeFilter_ShouldPassToSpecification()
    {
        // Arrange
        _operationLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentOperationLog>());

        _operationLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetOperationLogsQuery(OperationType: PaymentOperationType.InitiatePayment);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _operationLogRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PaymentOperationLogsSearchSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSuccessFilter_ShouldPassToSpecification()
    {
        // Arrange
        _operationLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentOperationLog>());

        _operationLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetOperationLogsQuery(Success: false);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _operationLogRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PaymentOperationLogsSearchSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCorrelationIdFilter_ShouldUseCorrelationSpec()
    {
        // Arrange
        var logs = CreateTestOperationLogs(2);

        _operationLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentOperationLogsByCorrelationIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        var query = new GetOperationLogsQuery(CorrelationId: "corr-test-123");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _operationLogRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PaymentOperationLogsByCorrelationIdSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDateRange_ShouldPassToSpecification()
    {
        // Arrange
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;

        _operationLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentOperationLog>());

        _operationLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetOperationLogsQuery(FromDate: fromDate, ToDate: toDate);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _operationLogRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PaymentOperationLogsSearchSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithFailedLog_ShouldIncludeErrorInfo()
    {
        // Arrange
        var log = CreateTestOperationLog(success: false);

        _operationLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentOperationLog> { log });

        _operationLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetOperationLogsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Items[0];
        dto.Success.Should().BeFalse();
        dto.ErrorCode.Should().Be("ERROR_CODE");
        dto.ErrorMessage.Should().Be("Something went wrong");
    }

    #endregion

    #region Empty Results Scenarios

    [Fact]
    public async Task Handle_WithNoLogs_ShouldReturnEmptyPagedResult()
    {
        // Arrange
        _operationLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentOperationLog>());

        _operationLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetOperationLogsQuery();

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

        _operationLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                token))
            .ReturnsAsync(new List<PaymentOperationLog>());

        _operationLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<PaymentOperationLogsSearchSpec>(),
                token))
            .ReturnsAsync(0);

        var query = new GetOperationLogsQuery();

        // Act
        await _handler.Handle(query, token);

        // Assert
        _operationLogRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PaymentOperationLogsSearchSpec>(), token),
            Times.Once);
        _operationLogRepositoryMock.Verify(
            x => x.CountAsync(It.IsAny<PaymentOperationLogsSearchSpec>(), token),
            Times.Once);
    }

    #endregion
}
