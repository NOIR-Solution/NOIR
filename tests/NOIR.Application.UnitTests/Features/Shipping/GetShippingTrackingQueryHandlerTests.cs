namespace NOIR.Application.UnitTests.Features.Shipping;

/// <summary>
/// Unit tests for GetShippingTrackingQueryHandler.
/// Tests tracking information retrieval scenarios.
/// </summary>
public class GetShippingTrackingQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ShippingOrder, Guid>> _orderRepositoryMock;
    private readonly Mock<IRepository<ShippingProvider, Guid>> _providerRepositoryMock;
    private readonly Mock<IShippingProviderFactory> _providerFactoryMock;
    private readonly Mock<IShippingProvider> _shippingProviderMock;
    private readonly Mock<ILogger<GetShippingTrackingQueryHandler>> _loggerMock;
    private readonly GetShippingTrackingQueryHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestTrackingNumber = "GHTK123456789";
    private static readonly Guid TestOrderId = Guid.NewGuid();
    private static readonly Guid TestProviderId = Guid.NewGuid();

    public GetShippingTrackingQueryHandlerTests()
    {
        _orderRepositoryMock = new Mock<IRepository<ShippingOrder, Guid>>();
        _providerRepositoryMock = new Mock<IRepository<ShippingProvider, Guid>>();
        _providerFactoryMock = new Mock<IShippingProviderFactory>();
        _shippingProviderMock = new Mock<IShippingProvider>();
        _loggerMock = new Mock<ILogger<GetShippingTrackingQueryHandler>>();

        _handler = new GetShippingTrackingQueryHandler(
            _orderRepositoryMock.Object,
            _providerRepositoryMock.Object,
            _providerFactoryMock.Object,
            _loggerMock.Object);
    }

    private static ShippingOrder CreateTestOrder(
        ShippingStatus status = ShippingStatus.InTransit,
        ShippingProviderCode providerCode = ShippingProviderCode.GHTK)
    {
        var order = ShippingOrder.Create(
            TestOrderId,
            TestProviderId,
            providerCode,
            "STANDARD",
            "Standard Delivery",
            "{}",
            "{}",
            "{}",
            "{}",
            "[]",
            1000m,
            500000m,
            200000m,
            false,
            "Test order",
            TestTenantId);

        // Set tracking number and status via reflection
        typeof(ShippingOrder).GetProperty("Id")?.SetValue(order, Guid.NewGuid());
        typeof(ShippingOrder).GetProperty("TrackingNumber")?.SetValue(order, TestTrackingNumber);
        typeof(ShippingOrder).GetProperty("Status")?.SetValue(order, status);
        typeof(ShippingOrder).GetProperty("TrackingUrl")?.SetValue(order, $"https://track.ghtk.vn/{TestTrackingNumber}");
        typeof(ShippingOrder).GetProperty("EstimatedDeliveryDate")?.SetValue(order, DateTimeOffset.UtcNow.AddDays(3));

        return order;
    }

    private static ShippingProvider CreateTestProvider()
    {
        var provider = ShippingProvider.Create(
            ShippingProviderCode.GHTK,
            "Test Provider",
            "GHTK Official",
            GatewayEnvironment.Sandbox,
            TestTenantId);

        provider.Activate();
        typeof(ShippingProvider).GetProperty("Id")?.SetValue(provider, TestProviderId);

        return provider;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithLocalDataOnly_ShouldReturnLocalTracking()
    {
        // Arrange
        var query = new GetShippingTrackingQuery(TestTrackingNumber);
        var order = CreateTestOrder(status: ShippingStatus.InTransit);
        var provider = CreateTestProvider();

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        // No provider implementation - will use local data
        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHTK))
            .Returns((IShippingProvider?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TrackingNumber.Should().Be(TestTrackingNumber);
        result.Value.ProviderCode.Should().Be(ShippingProviderCode.GHTK);
        result.Value.CurrentStatus.Should().Be(ShippingStatus.InTransit);
        result.Value.ProviderName.Should().Be("GHTK Official");
    }

    [Fact]
    public async Task Handle_WithProviderData_ShouldMergeTracking()
    {
        // Arrange
        var query = new GetShippingTrackingQuery(TestTrackingNumber);
        var order = CreateTestOrder(status: ShippingStatus.InTransit);
        var provider = CreateTestProvider();

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHTK))
            .Returns(_shippingProviderMock.Object);

        var providerTrackingInfo = new ProviderTrackingInfo(
            TestTrackingNumber,
            ShippingStatus.OutForDelivery,
            "Đang giao hàng đến người nhận",
            "Quận 1, TP.HCM",
            DateTimeOffset.UtcNow.AddDays(1),
            null,
            new List<ProviderTrackingEvent>
            {
                new ProviderTrackingEvent(
                    "OUT_FOR_DELIVERY",
                    ShippingStatus.OutForDelivery,
                    "Đang giao hàng",
                    "Quận 1",
                    DateTimeOffset.UtcNow)
            });

        _shippingProviderMock
            .Setup(x => x.GetTrackingAsync(TestTrackingNumber, It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(providerTrackingInfo));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TrackingNumber.Should().Be(TestTrackingNumber);
        result.Value.CurrentStatus.Should().Be(ShippingStatus.OutForDelivery); // From provider
        result.Value.CurrentLocation.Should().Be("Quận 1, TP.HCM");
    }

    [Fact]
    public async Task Handle_WhenProviderFails_ShouldFallbackToLocalData()
    {
        // Arrange
        var query = new GetShippingTrackingQuery(TestTrackingNumber);
        var order = CreateTestOrder(status: ShippingStatus.InTransit);
        var provider = CreateTestProvider();

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHTK))
            .Returns(_shippingProviderMock.Object);

        _shippingProviderMock
            .Setup(x => x.GetTrackingAsync(TestTrackingNumber, It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ProviderTrackingInfo>(Error.Failure("API_ERROR", "Provider unavailable")));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TrackingNumber.Should().Be(TestTrackingNumber);
        result.Value.CurrentStatus.Should().Be(ShippingStatus.InTransit); // Local status
    }

    [Fact]
    public async Task Handle_ForDraftOrder_ShouldNotFetchFromProvider()
    {
        // Arrange
        var query = new GetShippingTrackingQuery(TestTrackingNumber);
        var order = CreateTestOrder(status: ShippingStatus.Draft);
        var provider = CreateTestProvider();

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHTK))
            .Returns(_shippingProviderMock.Object);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentStatus.Should().Be(ShippingStatus.Draft);

        // Provider should not be called for Draft orders
        _shippingProviderMock.Verify(
            x => x.GetTrackingAsync(It.IsAny<string>(), It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenOrderNotFound_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetShippingTrackingQuery("NONEXISTENT123");

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingOrder?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Message.Should().Contain("NONEXISTENT123");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WhenProviderConfigMissing_ShouldUseProviderCodeAsName()
    {
        // Arrange
        var query = new GetShippingTrackingQuery(TestTrackingNumber);
        var order = CreateTestOrder(status: ShippingStatus.InTransit);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider?)null);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHTK))
            .Returns((IShippingProvider?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderName.Should().Be("GHTK"); // Falls back to enum name
    }

    [Fact]
    public async Task Handle_WhenProviderThrowsException_ShouldFallbackToLocalData()
    {
        // Arrange
        var query = new GetShippingTrackingQuery(TestTrackingNumber);
        var order = CreateTestOrder(status: ShippingStatus.InTransit);
        var provider = CreateTestProvider();

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHTK))
            .Returns(_shippingProviderMock.Object);

        _shippingProviderMock
            .Setup(x => x.GetTrackingAsync(TestTrackingNumber, It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - Should still succeed with local data
        result.IsSuccess.Should().BeTrue();
        result.Value.TrackingNumber.Should().Be(TestTrackingNumber);
        result.Value.CurrentStatus.Should().Be(ShippingStatus.InTransit);
    }

    [Theory]
    [InlineData(ShippingStatus.AwaitingPickup, "Đang chờ lấy hàng")]
    [InlineData(ShippingStatus.InTransit, "Đang vận chuyển")]
    [InlineData(ShippingStatus.Delivered, "Đã giao hàng thành công")]
    [InlineData(ShippingStatus.Cancelled, "Đã hủy")]
    public async Task Handle_ShouldReturnCorrectVietnameseStatusDescription(
        ShippingStatus status,
        string expectedDescription)
    {
        // Arrange
        var query = new GetShippingTrackingQuery(TestTrackingNumber);
        var order = CreateTestOrder(status: status);
        var provider = CreateTestProvider();

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHTK))
            .Returns((IShippingProvider?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StatusDescription.Should().Be(expectedDescription);
    }

    #endregion
}
