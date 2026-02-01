using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Queries.GetPaymentGateways;
using NOIR.Application.Features.Payments.Specifications;

namespace NOIR.Application.UnitTests.Features.Payments.Queries.GetPaymentGateways;

/// <summary>
/// Unit tests for GetPaymentGatewaysQueryHandler.
/// Tests retrieval of all payment gateways for admin view.
/// </summary>
public class GetPaymentGatewaysQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentGateway, Guid>> _gatewayRepositoryMock;
    private readonly GetPaymentGatewaysQueryHandler _handler;

    public GetPaymentGatewaysQueryHandlerTests()
    {
        _gatewayRepositoryMock = new Mock<IRepository<PaymentGateway, Guid>>();
        _handler = new GetPaymentGatewaysQueryHandler(_gatewayRepositoryMock.Object);
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
        gateway.SetWebhookUrl("https://api.example.com/webhooks/" + provider);
        if (hasCredentials)
            gateway.Configure("encrypted-credentials", "webhook-secret");
        if (isActive)
            gateway.Activate();
        return gateway;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithMultipleGateways_ShouldReturnAllGateways()
    {
        // Arrange
        var gateways = new List<PaymentGateway>
        {
            CreateTestGateway("vnpay", "VNPay", isActive: true, sortOrder: 1),
            CreateTestGateway("momo", "MoMo", isActive: true, sortOrder: 2),
            CreateTestGateway("zalopay", "ZaloPay", isActive: false, sortOrder: 3),
            CreateTestGateway("cod", "COD", isActive: true, sortOrder: 4)
        };

        _gatewayRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentGatewaysSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateways);

        var query = new GetPaymentGatewaysQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(4);
        result.Value[0].Provider.Should().Be("vnpay");
        result.Value[1].Provider.Should().Be("momo");
        result.Value[2].Provider.Should().Be("zalopay");
        result.Value[3].Provider.Should().Be("cod");
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var gateway = CreateTestGateway("vnpay", "VNPay", isActive: true, sortOrder: 1);
        gateway.UpdateHealthStatus(GatewayHealthStatus.Healthy);

        _gatewayRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentGatewaysSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentGateway> { gateway });

        var query = new GetPaymentGatewaysQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value[0];
        dto.Id.Should().Be(gateway.Id);
        dto.Provider.Should().Be("vnpay");
        dto.DisplayName.Should().Be("VNPay");
        dto.IsActive.Should().BeTrue();
        dto.SortOrder.Should().Be(1);
        dto.Environment.Should().Be(GatewayEnvironment.Sandbox);
        dto.HasCredentials.Should().BeTrue();
        dto.WebhookUrl.Should().Be("https://api.example.com/webhooks/vnpay");
        dto.MinAmount.Should().Be(10000);
        dto.MaxAmount.Should().Be(100000000);
        dto.SupportedCurrencies.Should().Be("[\"VND\"]");
        dto.HealthStatus.Should().Be(GatewayHealthStatus.Healthy);
    }

    [Fact]
    public async Task Handle_ShouldIncludeBothActiveAndInactiveGateways()
    {
        // Arrange
        var gateways = new List<PaymentGateway>
        {
            CreateTestGateway("vnpay", "VNPay", isActive: true),
            CreateTestGateway("momo", "MoMo", isActive: false)
        };

        _gatewayRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentGatewaysSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateways);

        var query = new GetPaymentGatewaysQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(g => g.IsActive == true);
        result.Value.Should().Contain(g => g.IsActive == false);
    }

    [Fact]
    public async Task Handle_WithSingleGateway_ShouldReturnSingleDto()
    {
        // Arrange
        var gateway = CreateTestGateway("cod", "Cash on Delivery");

        _gatewayRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentGatewaysSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentGateway> { gateway });

        var query = new GetPaymentGatewaysQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Provider.Should().Be("cod");
    }

    #endregion

    #region Empty Results Scenarios

    [Fact]
    public async Task Handle_WithNoGateways_ShouldReturnEmptyList()
    {
        // Arrange
        _gatewayRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentGatewaysSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentGateway>());

        var query = new GetPaymentGatewaysQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region CancellationToken Propagation

    [Fact]
    public async Task Handle_ShouldPropagateCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _gatewayRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<PaymentGatewaysSpec>(),
                token))
            .ReturnsAsync(new List<PaymentGateway>());

        var query = new GetPaymentGatewaysQuery();

        // Act
        await _handler.Handle(query, token);

        // Assert
        _gatewayRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PaymentGatewaysSpec>(), token),
            Times.Once);
    }

    #endregion
}
