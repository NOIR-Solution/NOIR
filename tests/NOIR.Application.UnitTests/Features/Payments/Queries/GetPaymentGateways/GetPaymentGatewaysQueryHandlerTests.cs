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
        result.IsSuccess.ShouldBe(true);
        result.Value.Count().ShouldBe(4);
        result.Value[0].Provider.ShouldBe("vnpay");
        result.Value[1].Provider.ShouldBe("momo");
        result.Value[2].Provider.ShouldBe("zalopay");
        result.Value[3].Provider.ShouldBe("cod");
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
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value[0];
        dto.Id.ShouldBe(gateway.Id);
        dto.Provider.ShouldBe("vnpay");
        dto.DisplayName.ShouldBe("VNPay");
        dto.IsActive.ShouldBe(true);
        dto.SortOrder.ShouldBe(1);
        dto.Environment.ShouldBe(GatewayEnvironment.Sandbox);
        dto.HasCredentials.ShouldBe(true);
        dto.WebhookUrl.ShouldBe("https://api.example.com/webhooks/vnpay");
        dto.MinAmount.ShouldBe(10000);
        dto.MaxAmount.ShouldBe(100000000);
        dto.SupportedCurrencies.ShouldBe("[\"VND\"]");
        dto.HealthStatus.ShouldBe(GatewayHealthStatus.Healthy);
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
        result.IsSuccess.ShouldBe(true);
        result.Value.Count().ShouldBe(2);
        result.Value.ShouldContain(g => g.IsActive == true);
        result.Value.ShouldContain(g => g.IsActive == false);
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
        result.IsSuccess.ShouldBe(true);
        result.Value.Count().ShouldBe(1);
        result.Value[0].Provider.ShouldBe("cod");
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
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBeEmpty();
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
