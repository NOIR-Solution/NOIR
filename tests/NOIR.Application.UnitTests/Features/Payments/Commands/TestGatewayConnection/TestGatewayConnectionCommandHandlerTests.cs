namespace NOIR.Application.UnitTests.Features.Payments.Commands.TestGatewayConnection;

using NOIR.Application.Features.Payments.Commands.TestGatewayConnection;
using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Specifications;

/// <summary>
/// Unit tests for TestGatewayConnectionCommandHandler.
/// Tests gateway connection testing scenarios with mocked dependencies.
/// </summary>
public class TestGatewayConnectionCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentGateway, Guid>> _gatewayRepositoryMock;
    private readonly Mock<IPaymentGatewayFactory> _gatewayFactoryMock;
    private readonly Mock<IPaymentGatewayProvider> _gatewayProviderMock;
    private readonly Mock<IPaymentOperationLogger> _operationLoggerMock;
    private readonly Mock<ILogger<TestGatewayConnectionCommandHandler>> _loggerMock;
    private readonly TestGatewayConnectionCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestProvider = "vnpay";
    private static readonly Guid TestGatewayId = Guid.NewGuid();

    public TestGatewayConnectionCommandHandlerTests()
    {
        _gatewayRepositoryMock = new Mock<IRepository<PaymentGateway, Guid>>();
        _gatewayFactoryMock = new Mock<IPaymentGatewayFactory>();
        _gatewayProviderMock = new Mock<IPaymentGatewayProvider>();
        _operationLoggerMock = new Mock<IPaymentOperationLogger>();
        _loggerMock = new Mock<ILogger<TestGatewayConnectionCommandHandler>>();

        // Default setup
        _operationLoggerMock.Setup(x => x.StartOperationAsync(
            It.IsAny<PaymentOperationType>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());

        _handler = new TestGatewayConnectionCommandHandler(
            _gatewayRepositoryMock.Object,
            _gatewayFactoryMock.Object,
            _operationLoggerMock.Object,
            _loggerMock.Object);
    }

    private static TestGatewayConnectionCommand CreateTestCommand(Guid? gatewayId = null)
    {
        return new TestGatewayConnectionCommand(gatewayId ?? TestGatewayId);
    }

    private static PaymentGateway CreateTestGateway(
        bool hasCredentials = true,
        string provider = TestProvider)
    {
        var gateway = PaymentGateway.Create(
            provider,
            "VNPay",
            GatewayEnvironment.Sandbox,
            TestTenantId);

        typeof(PaymentGateway).GetProperty("Id")?.SetValue(gateway, TestGatewayId);

        if (hasCredentials)
        {
            gateway.Configure("encrypted-credentials", "webhook-secret");
        }

        gateway.Activate();

        return gateway;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithHealthyGateway_ShouldReturnSuccess()
    {
        // Arrange
        var command = CreateTestCommand();
        var gateway = CreateTestGateway();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        _gatewayFactoryMock
            .Setup(x => x.GetProviderWithCredentialsAsync(TestProvider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_gatewayProviderMock.Object);

        _gatewayProviderMock
            .Setup(x => x.HealthCheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GatewayHealthStatus.Healthy);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Success.Should().BeTrue();
        result.Value.Message.Should().Be("Connection successful");
        result.Value.ResponseTimeMs.Should().NotBeNull();
        result.Value.ErrorCode.Should().BeNull();

        _operationLoggerMock.Verify(x => x.CompleteSuccessAsync(
            It.IsAny<Guid>(),
            It.IsAny<object?>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDegradedGateway_ShouldReturnSuccessWithWarning()
    {
        // Arrange
        var command = CreateTestCommand();
        var gateway = CreateTestGateway();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        _gatewayFactoryMock
            .Setup(x => x.GetProviderWithCredentialsAsync(TestProvider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_gatewayProviderMock.Object);

        _gatewayProviderMock
            .Setup(x => x.HealthCheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GatewayHealthStatus.Degraded);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.Message.Should().Contain("degraded");
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenGatewayNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = CreateTestCommand();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be(ErrorCodes.Payment.GatewayNotFound);

        _gatewayFactoryMock.Verify(x => x.GetProviderWithCredentialsAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNoCredentials_ShouldReturnNoCredentialsError()
    {
        // Arrange
        var command = CreateTestCommand();
        var gatewayWithoutCredentials = CreateTestGateway(hasCredentials: false);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gatewayWithoutCredentials);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        result.Value.ErrorCode.Should().Be("NO_CREDENTIALS");
        result.Value.Message.Should().Contain("no credentials configured");
    }

    [Fact]
    public async Task Handle_WhenProviderUnavailable_ShouldReturnProviderUnavailable()
    {
        // Arrange
        var command = CreateTestCommand();
        var gateway = CreateTestGateway();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        _gatewayFactoryMock
            .Setup(x => x.GetProviderWithCredentialsAsync(TestProvider, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IPaymentGatewayProvider?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        result.Value.ErrorCode.Should().Be("PROVIDER_UNAVAILABLE");
        result.Value.Message.Should().Contain("not available");

        _operationLoggerMock.Verify(x => x.CompleteFailedAsync(
            It.IsAny<Guid>(),
            "PROVIDER_UNAVAILABLE",
            It.IsAny<string?>(),
            It.IsAny<object?>(),
            It.IsAny<int?>(),
            It.IsAny<Exception?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnhealthyGateway_ShouldReturnUnhealthyError()
    {
        // Arrange
        var command = CreateTestCommand();
        var gateway = CreateTestGateway();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        _gatewayFactoryMock
            .Setup(x => x.GetProviderWithCredentialsAsync(TestProvider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_gatewayProviderMock.Object);

        _gatewayProviderMock
            .Setup(x => x.HealthCheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GatewayHealthStatus.Unhealthy);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        result.Value.ErrorCode.Should().Be("GATEWAY_UNHEALTHY");
        result.Value.Message.Should().Contain("unhealthy");
    }

    [Fact]
    public async Task Handle_WhenHttpRequestFails_ShouldReturnConnectionFailed()
    {
        // Arrange
        var command = CreateTestCommand();
        var gateway = CreateTestGateway();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        _gatewayFactoryMock
            .Setup(x => x.GetProviderWithCredentialsAsync(TestProvider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_gatewayProviderMock.Object);

        _gatewayProviderMock
            .Setup(x => x.HealthCheckAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network unreachable"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        result.Value.ErrorCode.Should().Be("CONNECTION_FAILED");
        result.Value.Message.Should().Contain("Network unreachable");
    }

    [Fact]
    public async Task Handle_WhenTimeoutOccurs_ShouldReturnTimeoutError()
    {
        // Arrange
        var command = CreateTestCommand();
        var gateway = CreateTestGateway();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        _gatewayFactoryMock
            .Setup(x => x.GetProviderWithCredentialsAsync(TestProvider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_gatewayProviderMock.Object);

        _gatewayProviderMock
            .Setup(x => x.HealthCheckAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        result.Value.ErrorCode.Should().Be("TIMEOUT");
        result.Value.Message.Should().Contain("timed out");
    }

    [Fact]
    public async Task Handle_WhenUnexpectedErrorOccurs_ShouldReturnUnexpectedError()
    {
        // Arrange
        var command = CreateTestCommand();
        var gateway = CreateTestGateway();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        _gatewayFactoryMock
            .Setup(x => x.GetProviderWithCredentialsAsync(TestProvider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_gatewayProviderMock.Object);

        _gatewayProviderMock
            .Setup(x => x.HealthCheckAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        result.Value.ErrorCode.Should().Be("UNEXPECTED_ERROR");
        result.Value.Message.Should().Contain("Unexpected error");
    }

    #endregion
}
