using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Queries.GetActiveGateways;
using NOIR.Application.Features.Payments.Specifications;

namespace NOIR.Application.UnitTests.Features.Payments.Queries.GetActiveGateways;

/// <summary>
/// Unit tests for GetActiveGatewaysQueryHandler.
/// Tests retrieval of active payment gateways for checkout display.
/// </summary>
public class GetActiveGatewaysQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentGateway, Guid>> _gatewayRepositoryMock;
    private readonly GetActiveGatewaysQueryHandler _handler;

    public GetActiveGatewaysQueryHandlerTests()
    {
        _gatewayRepositoryMock = new Mock<IRepository<PaymentGateway, Guid>>();
        _handler = new GetActiveGatewaysQueryHandler(_gatewayRepositoryMock.Object);
    }

    private static PaymentGateway CreateTestGateway(
        string provider = "vnpay",
        string displayName = "VNPay",
        bool isActive = true,
        int sortOrder = 0)
    {
        var gateway = PaymentGateway.Create(provider, displayName, GatewayEnvironment.Sandbox, "tenant-123");
        gateway.SetSortOrder(sortOrder);
        gateway.SetAmountLimits(10000, 100000000);
        gateway.SetSupportedCurrencies("[\"VND\"]");
        if (isActive)
            gateway.Activate();
        return gateway;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithActiveGateways_ShouldReturnCheckoutGatewayDtos()
    {
        // Arrange
        var gateways = new List<PaymentGateway>
        {
            CreateTestGateway("vnpay", "VNPay", isActive: true, sortOrder: 1),
            CreateTestGateway("momo", "MoMo", isActive: true, sortOrder: 2),
            CreateTestGateway("zalopay", "ZaloPay", isActive: true, sortOrder: 3)
        };

        _gatewayRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ActivePaymentGatewaysSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateways);

        var query = new GetActiveGatewaysQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].Provider.Should().Be("vnpay");
        result.Value[0].DisplayName.Should().Be("VNPay");
        result.Value[1].Provider.Should().Be("momo");
        result.Value[2].Provider.Should().Be("zalopay");
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var gateway = CreateTestGateway("vnpay", "VNPay", isActive: true, sortOrder: 5);

        _gatewayRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ActivePaymentGatewaysSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentGateway> { gateway });

        var query = new GetActiveGatewaysQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value[0];
        dto.Id.Should().Be(gateway.Id);
        dto.Provider.Should().Be("vnpay");
        dto.DisplayName.Should().Be("VNPay");
        dto.SortOrder.Should().Be(5);
        dto.MinAmount.Should().Be(10000);
        dto.MaxAmount.Should().Be(100000000);
        dto.SupportedCurrencies.Should().Be("[\"VND\"]");
    }

    [Fact]
    public async Task Handle_WithSingleGateway_ShouldReturnSingleDto()
    {
        // Arrange
        var gateway = CreateTestGateway("cod", "Cash on Delivery", isActive: true);

        _gatewayRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ActivePaymentGatewaysSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentGateway> { gateway });

        var query = new GetActiveGatewaysQuery();

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
    public async Task Handle_WithNoActiveGateways_ShouldReturnEmptyList()
    {
        // Arrange
        _gatewayRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ActivePaymentGatewaysSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentGateway>());

        var query = new GetActiveGatewaysQuery();

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
                It.IsAny<ActivePaymentGatewaysSpec>(),
                token))
            .ReturnsAsync(new List<PaymentGateway>());

        var query = new GetActiveGatewaysQuery();

        // Act
        await _handler.Handle(query, token);

        // Assert
        _gatewayRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ActivePaymentGatewaysSpec>(), token),
            Times.Once);
    }

    #endregion
}
