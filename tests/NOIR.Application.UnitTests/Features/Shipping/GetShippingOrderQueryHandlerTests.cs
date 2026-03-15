using NOIR.Application.Features.Shipping.DTOs;
using NOIR.Application.Features.Shipping.Queries.GetShippingOrder;
using NOIR.Application.Features.Shipping.Specifications;

namespace NOIR.Application.UnitTests.Features.Shipping;

/// <summary>
/// Unit tests for GetShippingOrderQueryHandler.
/// Tests shipping order retrieval by tracking number, ID, and order ID.
/// </summary>
public class GetShippingOrderQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ShippingOrder, Guid>> _orderRepositoryMock;
    private readonly Mock<IRepository<ShippingProvider, Guid>> _providerRepositoryMock;
    private readonly GetShippingOrderQueryHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestTrackingNumber = "GHTK123456789";

    public GetShippingOrderQueryHandlerTests()
    {
        _orderRepositoryMock = new Mock<IRepository<ShippingOrder, Guid>>();
        _providerRepositoryMock = new Mock<IRepository<ShippingProvider, Guid>>();

        _handler = new GetShippingOrderQueryHandler(
            _orderRepositoryMock.Object,
            _providerRepositoryMock.Object);
    }

    private static ShippingOrder CreateTestOrder(
        Guid? id = null,
        Guid? orderId = null,
        string trackingNumber = TestTrackingNumber,
        ShippingProviderCode providerCode = ShippingProviderCode.GHTK,
        ShippingStatus status = ShippingStatus.AwaitingPickup,
        string? tenantId = TestTenantId)
    {
        var order = ShippingOrder.Create(
            orderId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            providerCode,
            "STANDARD",
            "Standard Delivery",
            "{}",
            "{}",
            "{}",
            "{}",
            "[]",
            1500m,
            1000000m,
            null,
            false,
            null,
            tenantId);

        order.SetProviderResponse(
            trackingNumber,
            "PROVIDER_ORDER_123",
            "https://label.example.com/123",
            "https://tracking.example.com/123",
            30000m,
            0m,
            0m,
            DateTimeOffset.UtcNow.AddDays(3),
            "{}");

        if (id.HasValue)
        {
            typeof(Entity<Guid>).GetProperty("Id")!.SetValue(order, id.Value);
        }

        return order;
    }

    private static ShippingProvider CreateTestProvider(
        Guid? id = null,
        ShippingProviderCode providerCode = ShippingProviderCode.GHTK)
    {
        var provider = ShippingProvider.Create(
            providerCode,
            "Test Provider",
            "Giao Hang Tiet Kiem",
            GatewayEnvironment.Sandbox,
            TestTenantId);

        if (id.HasValue)
        {
            typeof(Entity<Guid>).GetProperty("Id")!.SetValue(provider, id.Value);
        }

        return provider;
    }

    #endregion

    #region GetShippingOrderQuery (By Tracking Number) Tests

    [Fact]
    public async Task Handle_ByTrackingNumber_ValidTrackingNumber_ReturnsOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = CreateTestOrder(orderId, trackingNumber: TestTrackingNumber);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var query = new GetShippingOrderQuery(TestTrackingNumber);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.TrackingNumber.ShouldBe(TestTrackingNumber);
        result.Value.ProviderCode.ShouldBe(ShippingProviderCode.GHTK);
    }

    [Fact]
    public async Task Handle_ByTrackingNumber_OrderNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingOrder?)null);

        var query = new GetShippingOrderQuery("NONEXISTENT123");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Message.ShouldContain("NONEXISTENT123");
    }

    [Fact]
    public async Task Handle_ByTrackingNumber_ReturnsProviderName()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId);
        var order = CreateTestOrder();

        // Set up the Provider navigation property
        typeof(ShippingOrder).GetProperty("Provider")!.SetValue(order, provider);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var query = new GetShippingOrderQuery(TestTrackingNumber);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ProviderName.ShouldBe("Giao Hang Tiet Kiem");
    }

    #endregion

    #region GetShippingOrderByIdQuery Tests

    [Fact]
    public async Task Handle_ById_ValidId_ReturnsOrder()
    {
        // Arrange
        var shippingOrderId = Guid.NewGuid();
        var order = CreateTestOrder(shippingOrderId);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var query = new GetShippingOrderByIdQuery(shippingOrderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBe(shippingOrderId);
    }

    [Fact]
    public async Task Handle_ById_OrderNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingOrder?)null);

        var query = new GetShippingOrderByIdQuery(nonExistentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Message.ShouldContain(nonExistentId.ToString());
    }

    #endregion

    #region GetShippingOrderByOrderIdQuery Tests

    [Fact]
    public async Task Handle_ByOrderId_ValidOrderId_ReturnsOrder()
    {
        // Arrange
        var noirOrderId = Guid.NewGuid();
        var shippingOrderId = Guid.NewGuid();
        var order = CreateTestOrder(shippingOrderId, orderId: noirOrderId);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByOrderIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var query = new GetShippingOrderByOrderIdQuery(noirOrderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.OrderId.ShouldBe(noirOrderId);
    }

    [Fact]
    public async Task Handle_ByOrderId_OrderNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var nonExistentOrderId = Guid.NewGuid();

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByOrderIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingOrder?)null);

        var query = new GetShippingOrderByOrderIdQuery(nonExistentOrderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Message.ShouldContain(nonExistentOrderId.ToString());
    }

    #endregion

    #region DTO Mapping Tests

    [Fact]
    public async Task Handle_ReturnsCorrectDtoFields()
    {
        // Arrange
        var shippingOrderId = Guid.NewGuid();
        var noirOrderId = Guid.NewGuid();
        var order = CreateTestOrder(shippingOrderId, orderId: noirOrderId);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var query = new GetShippingOrderQuery(TestTrackingNumber);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value;

        dto.Id.ShouldBe(shippingOrderId);
        dto.OrderId.ShouldBe(noirOrderId);
        dto.ProviderCode.ShouldBe(ShippingProviderCode.GHTK);
        dto.TrackingNumber.ShouldBe(TestTrackingNumber);
        dto.ServiceTypeCode.ShouldBe("STANDARD");
        dto.ServiceTypeName.ShouldBe("Standard Delivery");
        dto.Status.ShouldBe(ShippingStatus.AwaitingPickup);
        dto.BaseRate.ShouldBe(30000m);
        dto.TotalShippingFee.ShouldBe(30000m);
        dto.LabelUrl.ShouldBe("https://label.example.com/123");
        dto.TrackingUrl.ShouldBe("https://tracking.example.com/123");
        dto.EstimatedDeliveryDate.ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_WithoutProvider_UsesProviderCodeAsProviderName()
    {
        // Arrange
        var order = CreateTestOrder();
        // Provider navigation property is null

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var query = new GetShippingOrderQuery(TestTrackingNumber);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ProviderName.ShouldBe("GHTK"); // Falls back to ProviderCode.ToString()
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_ByTrackingNumber_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var order = CreateTestOrder();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var query = new GetShippingOrderQuery(TestTrackingNumber);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _orderRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ById_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var order = CreateTestOrder();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var query = new GetShippingOrderByIdQuery(Guid.NewGuid());

        // Act
        await _handler.Handle(query, token);

        // Assert
        _orderRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByIdSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ByOrderId_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var order = CreateTestOrder();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByOrderIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var query = new GetShippingOrderByOrderIdQuery(Guid.NewGuid());

        // Act
        await _handler.Handle(query, token);

        // Assert
        _orderRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByOrderIdSpec>(), token),
            Times.Once);
    }

    #endregion
}
