using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Queries.GetPaymentGateway;
using NOIR.Application.Features.Payments.Specifications;

namespace NOIR.Application.UnitTests.Features.Payments.Queries.GetPaymentGateway;

/// <summary>
/// Unit tests for GetPaymentGatewayQueryHandler.
/// Tests retrieval of a single payment gateway by ID.
/// </summary>
public class GetPaymentGatewayQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentGateway, Guid>> _gatewayRepositoryMock;
    private readonly GetPaymentGatewayQueryHandler _handler;

    public GetPaymentGatewayQueryHandlerTests()
    {
        _gatewayRepositoryMock = new Mock<IRepository<PaymentGateway, Guid>>();
        _handler = new GetPaymentGatewayQueryHandler(_gatewayRepositoryMock.Object);
    }

    private static PaymentGateway CreateTestGateway(
        string provider = "vnpay",
        string displayName = "VNPay",
        bool isActive = true,
        int sortOrder = 0,
        bool hasCredentials = true)
    {
        var gateway = PaymentGateway.Create(provider, displayName, GatewayEnvironment.Sandbox, "tenant-123");
        gateway.SetSortOrder(sortOrder);
        gateway.SetAmountLimits(10000, 100000000);
        gateway.SetSupportedCurrencies("[\"VND\"]");
        gateway.SetWebhookUrl("https://api.example.com/webhooks/vnpay");
        if (hasCredentials)
            gateway.Configure("encrypted-credentials", "webhook-secret");
        if (isActive)
            gateway.Activate();
        return gateway;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenGatewayExists_ShouldReturnPaymentGatewayDto()
    {
        // Arrange
        var gatewayId = Guid.NewGuid();
        var gateway = CreateTestGateway("vnpay", "VNPay", isActive: true, sortOrder: 1);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PaymentGatewayByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        var query = new GetPaymentGatewayQuery(gatewayId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Provider.Should().Be("vnpay");
        result.Value.DisplayName.Should().Be("VNPay");
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var gateway = CreateTestGateway("momo", "MoMo", isActive: true, sortOrder: 5, hasCredentials: true);
        gateway.UpdateHealthStatus(GatewayHealthStatus.Healthy);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PaymentGatewayByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        var query = new GetPaymentGatewayQuery(gateway.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Id.Should().Be(gateway.Id);
        dto.Provider.Should().Be("momo");
        dto.DisplayName.Should().Be("MoMo");
        dto.IsActive.Should().BeTrue();
        dto.SortOrder.Should().Be(5);
        dto.Environment.Should().Be(GatewayEnvironment.Sandbox);
        dto.HasCredentials.Should().BeTrue();
        dto.WebhookUrl.Should().Be("https://api.example.com/webhooks/vnpay");
        dto.MinAmount.Should().Be(10000);
        dto.MaxAmount.Should().Be(100000000);
        dto.SupportedCurrencies.Should().Be("[\"VND\"]");
        dto.HealthStatus.Should().Be(GatewayHealthStatus.Healthy);
        dto.LastHealthCheck.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithInactiveGateway_ShouldReturnInactiveFlag()
    {
        // Arrange
        var gateway = CreateTestGateway("zalopay", "ZaloPay", isActive: false);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PaymentGatewayByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        var query = new GetPaymentGatewayQuery(gateway.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithNoCredentials_ShouldReturnHasCredentialsFalse()
    {
        // Arrange
        var gateway = CreateTestGateway("cod", "COD", hasCredentials: false);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PaymentGatewayByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateway);

        var query = new GetPaymentGatewayQuery(gateway.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasCredentials.Should().BeFalse();
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenGatewayNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var gatewayId = Guid.NewGuid();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PaymentGatewayByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway?)null);

        var query = new GetPaymentGatewayQuery(gatewayId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Payment.GatewayNotFound);
    }

    #endregion

    #region CancellationToken Propagation

    [Fact]
    public async Task Handle_ShouldPropagateCancellationToken()
    {
        // Arrange
        var gatewayId = Guid.NewGuid();
        var gateway = CreateTestGateway();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<PaymentGatewayByIdSpec>(),
                token))
            .ReturnsAsync(gateway);

        var query = new GetPaymentGatewayQuery(gatewayId);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _gatewayRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByIdSpec>(), token),
            Times.Once);
    }

    #endregion
}
