using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Queries.GetWebhookLogs;
using NOIR.Application.Features.Payments.Specifications;

namespace NOIR.Application.UnitTests.Features.Payments.Queries.GetWebhookLogs;

/// <summary>
/// Unit tests for GetWebhookLogsQueryHandler.
/// Tests paginated retrieval of webhook logs with filtering.
/// </summary>
public class GetWebhookLogsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentWebhookLog, Guid>> _webhookLogRepositoryMock;
    private readonly GetWebhookLogsQueryHandler _handler;

    public GetWebhookLogsQueryHandlerTests()
    {
        _webhookLogRepositoryMock = new Mock<IRepository<PaymentWebhookLog, Guid>>();
        _handler = new GetWebhookLogsQueryHandler(_webhookLogRepositoryMock.Object);
    }

    private static PaymentWebhookLog CreateTestWebhookLog(
        string provider = "vnpay",
        string eventType = "payment.success",
        WebhookProcessingStatus status = WebhookProcessingStatus.Processed)
    {
        var gatewayId = Guid.NewGuid();
        var log = PaymentWebhookLog.Create(
            gatewayId,
            provider,
            eventType,
            "{\"transaction_id\":\"123\"}",
            "tenant-123");
        log.SetGatewayEventId("event-123");
        log.SetRequestDetails("{\"x-signature\":\"abc\"}", "signature-value", "192.168.1.1");
        log.MarkSignatureValid(true);

        if (status == WebhookProcessingStatus.Processed)
            log.MarkAsProcessed(Guid.NewGuid());
        else if (status == WebhookProcessingStatus.Failed)
            log.MarkAsFailed("Processing error");

        return log;
    }

    private static List<PaymentWebhookLog> CreateTestWebhookLogs(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestWebhookLog(
                "vnpay",
                $"payment.event.{i}",
                i % 2 == 0 ? WebhookProcessingStatus.Processed : WebhookProcessingStatus.Failed))
            .ToList();
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDefaultPaging_ShouldReturnPagedResult()
    {
        // Arrange
        var logs = CreateTestWebhookLogs(5);

        _webhookLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<WebhookLogsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        _webhookLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<WebhookLogsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var query = new GetWebhookLogsQuery();

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
        var page2Logs = CreateTestWebhookLogs(10);

        _webhookLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<WebhookLogsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page2Logs);

        _webhookLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<WebhookLogsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        var query = new GetWebhookLogsQuery(Page: 2, PageSize: 10);

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
        var log = CreateTestWebhookLog("momo", "payment.completed", WebhookProcessingStatus.Processed);

        _webhookLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<WebhookLogsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentWebhookLog> { log });

        _webhookLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<WebhookLogsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetWebhookLogsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Items[0];
        dto.Id.Should().Be(log.Id);
        dto.PaymentGatewayId.Should().NotBeEmpty();
        dto.Provider.Should().Be("momo");
        dto.EventType.Should().Be("payment.completed");
        dto.GatewayEventId.Should().Be("event-123");
        dto.SignatureValid.Should().BeTrue();
        dto.ProcessingStatus.Should().Be(WebhookProcessingStatus.Processed);
        dto.PaymentTransactionId.Should().NotBeNull();
        dto.IpAddress.Should().Be("192.168.1.1");
        dto.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithProviderFilter_ShouldPassToSpecification()
    {
        // Arrange
        _webhookLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<WebhookLogsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentWebhookLog>());

        _webhookLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<WebhookLogsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetWebhookLogsQuery(Provider: "vnpay");

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _webhookLogRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<WebhookLogsSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldPassToSpecification()
    {
        // Arrange
        _webhookLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<WebhookLogsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentWebhookLog>());

        _webhookLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<WebhookLogsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetWebhookLogsQuery(Status: WebhookProcessingStatus.Failed);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _webhookLogRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<WebhookLogsSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithFailedLog_ShouldIncludeErrorInfo()
    {
        // Arrange
        var log = CreateTestWebhookLog(status: WebhookProcessingStatus.Failed);

        _webhookLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<WebhookLogsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentWebhookLog> { log });

        _webhookLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<WebhookLogsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetWebhookLogsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Items[0];
        dto.ProcessingStatus.Should().Be(WebhookProcessingStatus.Failed);
        dto.ProcessingError.Should().Be("Processing error");
        dto.RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithCustomPageSize_ShouldApplyPageSize()
    {
        // Arrange
        var logs = CreateTestWebhookLogs(5);

        _webhookLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<WebhookLogsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        _webhookLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<WebhookLogsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        var query = new GetWebhookLogsQuery(Page: 1, PageSize: 5);

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
    public async Task Handle_WithNoLogs_ShouldReturnEmptyPagedResult()
    {
        // Arrange
        _webhookLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<WebhookLogsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentWebhookLog>());

        _webhookLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<WebhookLogsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetWebhookLogsQuery();

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

        _webhookLogRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<WebhookLogsSpec>(),
                token))
            .ReturnsAsync(new List<PaymentWebhookLog>());

        _webhookLogRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<WebhookLogsSpec>(),
                token))
            .ReturnsAsync(0);

        var query = new GetWebhookLogsQuery();

        // Act
        await _handler.Handle(query, token);

        // Assert
        _webhookLogRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<WebhookLogsSpec>(), token),
            Times.Once);
        _webhookLogRepositoryMock.Verify(
            x => x.CountAsync(It.IsAny<WebhookLogsSpec>(), token),
            Times.Once);
    }

    #endregion
}
